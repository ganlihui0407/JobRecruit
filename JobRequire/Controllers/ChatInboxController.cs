using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobRequire.Services;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// MVC Controller for Chat Inbox page.
    /// </summary>
    [Authorize]
    public class ChatInboxController : Controller
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatInboxController> _logger;

        public ChatInboxController(IChatService chatService, ILogger<ChatInboxController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Displays the Chat Inbox page with list of chat threads.
        /// </summary>
        [HttpGet]
        [Route("Chat/Inbox")]
        public async Task<IActionResult> Inbox()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            // Only JobSeeker and Employer can access chat inbox
            if (userRole != "JobSeeker" && userRole != "Employer")
            {
                TempData["ErrorMessage"] = "Chat inbox is only available for Job Seekers and Employers.";
                return RedirectToAction("Index", "Home");
            }

            var chatThreads = await _chatService.GetChatInboxAsync(userId.Value, userRole);
            
            ViewBag.UserRole = userRole;
            return View(chatThreads);
        }

        /// <summary>
        /// Marks all messages in a thread as read and redirects to the unified application chat page.
        /// </summary>
        [HttpGet]
        [Route("Chat/Open/{jobApplicationId}")]
        public async Task<IActionResult> OpenChat(int jobApplicationId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            // Validate access
            if (!await _chatService.ValidateUserAccessAsync(jobApplicationId, userId.Value, userRole))
            {
                TempData["ErrorMessage"] = "You do not have access to this chat.";
                return RedirectToAction("Inbox");
            }

            // Redirect to unified chat page (mark-as-read is handled there)
            return RedirectToAction("Chat", "Application", new { applicationId = jobApplicationId });
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
    }
}


