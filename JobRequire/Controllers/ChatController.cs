using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobRequire.Models;
using JobRequire.Services;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for managing application chat functionality.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Gets chat status and messages for a job application.
        /// </summary>
        [HttpGet("{jobApplicationId}")]
        public async Task<IActionResult> GetMessages(int jobApplicationId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized();
            }

            // Validate access
            if (!await _chatService.ValidateUserAccessAsync(jobApplicationId, userId.Value, userRole))
            {
                return Forbid();
            }

            // Check chat availability
            var isAvailable = await _chatService.IsChatAvailableAsync(jobApplicationId);
            var isReadOnly = await _chatService.IsChatReadOnlyAsync(jobApplicationId);
            var status = await _chatService.GetApplicationStatusAsync(jobApplicationId);

            if (!isAvailable)
            {
                return Ok(new ChatResponse
                {
                    IsAvailable = false,
                    IsReadOnly = true,
                    ApplicationStatus = status,
                    Messages = new List<ChatMessageDto>(),
                    Message = "Chat is not available for pending applications."
                });
            }

            // Get or create chat thread (this will create system message if new)
            var thread = await _chatService.GetOrCreateChatThreadAsync(jobApplicationId);
            var messages = await _chatService.GetMessagesAsync(jobApplicationId);

            return Ok(new ChatResponse
            {
                IsAvailable = true,
                IsReadOnly = isReadOnly,
                ApplicationStatus = status,
                Messages = messages.Select(m => new ChatMessageDto
                {
                    ChatMessageId = m.ChatMessageId,
                    SenderRole = m.SenderRole,
                    Message = m.Message,
                    // Format as ISO 8601 with UTC marker for proper client-side local time conversion
                    CreatedAt = m.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    IsCurrentUser = GetSenderRoleForUser(userRole) == m.SenderRole
                }).ToList(),
                Message = isReadOnly ? "This chat is read-only because the application has been rejected or withdrawn." : null
            });
        }

        /// <summary>
        /// Sends a message in the chat thread.
        /// </summary>
        [HttpPost("{jobApplicationId}")]
        public async Task<IActionResult> PostMessage(int jobApplicationId, [FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new { error = "Message cannot be empty." });
            }

            if (request.Message.Length > 2000)
            {
                return BadRequest(new { error = "Message cannot exceed 2000 characters." });
            }

            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized();
            }

            // Validate access
            if (!await _chatService.ValidateUserAccessAsync(jobApplicationId, userId.Value, userRole))
            {
                return Forbid();
            }

            // Check if chat is available
            if (!await _chatService.IsChatAvailableAsync(jobApplicationId))
            {
                return BadRequest(new { error = "Chat is not available for this application." });
            }

            // Check if chat is read-only
            if (await _chatService.IsChatReadOnlyAsync(jobApplicationId))
            {
                return BadRequest(new { error = "Chat is read-only. The application has been rejected or withdrawn." });
            }

            // Send message
            var senderRole = GetSenderRoleForUser(userRole);
            var message = await _chatService.SendMessageAsync(jobApplicationId, senderRole, request.Message);

            if (message == null)
            {
                return BadRequest(new { error = "Failed to send message." });
            }

            return Ok(new ChatMessageDto
            {
                ChatMessageId = message.ChatMessageId,
                SenderRole = message.SenderRole,
                Message = message.Message,
                // Format as ISO 8601 with UTC marker for proper client-side local time conversion
                CreatedAt = message.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                IsCurrentUser = true
            });
        }

        /// <summary>
        /// Gets unread chat message count for the current user.
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return Ok(new { unreadCount = 0 });
            }

            var count = await _chatService.GetUnreadCountAsync(userId.Value, userRole);
            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// Marks all messages in a thread as read for the current user.
        /// </summary>
        [HttpPost("{jobApplicationId}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int jobApplicationId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized();
            }

            // Validate access
            if (!await _chatService.ValidateUserAccessAsync(jobApplicationId, userId.Value, userRole))
            {
                return Forbid();
            }

            await _chatService.MarkThreadAsReadAsync(jobApplicationId, userRole);
            return Ok(new { success = true });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        private string GetSenderRoleForUser(string userRole)
        {
            return userRole switch
            {
                "Employer" => ChatSenderRole.Employer,
                "JobSeeker" => ChatSenderRole.JobSeeker,
                "Admin" => ChatSenderRole.System, // Admin messages appear as system
                _ => ChatSenderRole.System
            };
        }
    }

    // DTOs for Chat API
    public class ChatResponse
    {
        public bool IsAvailable { get; set; }
        public bool IsReadOnly { get; set; }
        public string? ApplicationStatus { get; set; }
        public string? Message { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public class ChatMessageDto
    {
        public int ChatMessageId { get; set; }
        public string SenderRole { get; set; } = null!;
        public string Message { get; set; } = null!;
        /// <summary>
        /// ISO 8601 formatted timestamp with UTC marker (Z suffix) for proper client-side conversion.
        /// </summary>
        public string CreatedAt { get; set; } = null!;
        public bool IsCurrentUser { get; set; }
    }

    public class SendMessageRequest
    {
        public string Message { get; set; } = null!;
    }
}


