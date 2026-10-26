using Microsoft.AspNetCore.Mvc;
using JobRequire.Services;
using System.Security.Claims;

namespace JobRequire.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying chat icon with unread count in the navbar.
    /// Works for JobSeeker and Employer users only.
    /// </summary>
    public class ChatViewComponent : ViewComponent
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatViewComponent> _logger;

        public ChatViewComponent(IChatService chatService, ILogger<ChatViewComponent> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var viewModel = new ChatSummaryViewModel();

            try
            {
                // Get current user ID and role from claims
                var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return View(viewModel);
                }

                // Only show chat icon for JobSeeker and Employer
                if (userRole != "JobSeeker" && userRole != "Employer")
                {
                    viewModel.ShowIcon = false;
                    return View(viewModel);
                }

                viewModel.ShowIcon = true;
                viewModel.UnreadCount = await _chatService.GetUnreadCountAsync(userId, userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat unread count for ViewComponent");
            }

            return View(viewModel);
        }
    }

    /// <summary>
    /// ViewModel for the chat icon in navbar
    /// </summary>
    public class ChatSummaryViewModel
    {
        public int UnreadCount { get; set; }
        public bool ShowIcon { get; set; } = true;
        public bool HasUnread => UnreadCount > 0;
    }
}






