using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using System.Security.Claims;

namespace JobRequire.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying notifications in the navbar
    /// Works for all authenticated users (Employer, JobSeeker, Admin)
    /// </summary>
    public class NotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationViewComponent> _logger;

        public NotificationViewComponent(ApplicationDbContext context, ILogger<NotificationViewComponent> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var viewModel = new NotificationSummaryViewModel();

            try
            {
                // Get current user ID from claims (works for all roles)
                var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return View(viewModel);
                }

                // Get unread count
                viewModel.UnreadCount = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .CountAsync();

                // Get latest 5 unread notifications for dropdown
                viewModel.RecentNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new NotificationItemViewModel
                    {
                        NotificationId = n.NotificationId,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        ActionUrl = n.ActionUrl,
                        ActionText = n.ActionText,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead
                    })
                    .ToListAsync();

                // Calculate time ago for each notification
                foreach (var notification in viewModel.RecentNotifications)
                {
                    notification.TimeAgo = GetTimeAgo(notification.CreatedAt);
                }

                // Get latest notification (read or unread) for preview
                viewModel.LatestNotification = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationItemViewModel
                    {
                        NotificationId = n.NotificationId,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        ActionUrl = n.ActionUrl,
                        ActionText = n.ActionText,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead
                    })
                    .FirstOrDefaultAsync();

                if (viewModel.LatestNotification != null)
                {
                    viewModel.LatestNotification.TimeAgo = GetTimeAgo(viewModel.LatestNotification.CreatedAt);
                }

                viewModel.HasNotifications = viewModel.RecentNotifications.Any() || viewModel.LatestNotification != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications for ViewComponent");
            }

            return View(viewModel);
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;

            if (span.TotalDays >= 30)
                return $"{(int)(span.TotalDays / 30)}mo ago";
            if (span.TotalDays >= 7)
                return $"{(int)(span.TotalDays / 7)}w ago";
            if (span.TotalDays >= 1)
                return $"{(int)span.TotalDays}d ago";
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h ago";
            if (span.TotalMinutes >= 1)
                return $"{(int)span.TotalMinutes}m ago";
            return "Just now";
        }
    }

    /// <summary>
    /// ViewModel for the notification summary in navbar
    /// </summary>
    public class NotificationSummaryViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationItemViewModel> RecentNotifications { get; set; } = new();
        public NotificationItemViewModel? LatestNotification { get; set; }
        public bool HasNotifications { get; set; }
        public bool HasUnread => UnreadCount > 0;
    }

    /// <summary>
    /// ViewModel for individual notification items
    /// </summary>
    public class NotificationItemViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string Type { get; set; } = "Info";
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string TimeAgo { get; set; } = string.Empty;

        /// <summary>
        /// Get Bootstrap icon class based on notification type
        /// </summary>
        public string IconClass => Type.ToLower() switch
        {
            "success" => "bi-check-circle-fill text-success",
            "warning" => "bi-exclamation-triangle-fill text-warning",
            "error" => "bi-x-circle-fill text-danger",
            "system" => "bi-gear-fill text-secondary",
            _ => "bi-info-circle-fill text-primary"
        };

        /// <summary>
        /// Get background class for unread notifications
        /// </summary>
        public string BackgroundClass => IsRead ? "" : "bg-light";
    }
}






























































