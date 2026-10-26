using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Services;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for viewing and managing notifications
    /// </summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// GET: Notifications - View all notifications
        /// </summary>
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();

            var notifications = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.ActionUrl,
                    n.ActionText,
                    n.IsRead,
                    n.CreatedAt,
                    n.ReadAt
                })
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.UnreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);

            return View(notifications.Select(n => new NotificationViewModel
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ActionUrl = n.ActionUrl,
                ActionText = n.ActionText,
                CreatedAt = n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt),
                IsRead = n.IsRead
            }).ToList());
        }

        /// <summary>
        /// GET: Notifications/GoTo/{id} - Mark notification as read and redirect to action URL
        /// </summary>
        public async Task<IActionResult> GoTo(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            // Get the notification
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification == null)
            {
                TempData["ErrorMessage"] = "Notification not found.";
                return RedirectToAction(nameof(Index));
            }

            // Mark as read if not already
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Redirect to the action URL or fall back to Index
            if (!string.IsNullOrEmpty(notification.ActionUrl))
            {
                var actionUrl = notification.ActionUrl;

                // Fix old notification URLs that point to list page instead of detail page
                // Old format: /JobApplications/MyApplications
                // Should redirect to: /JobApplications/MyApplicationDetails/{id}
                if (actionUrl.Contains("/JobApplications/MyApplications") && 
                    !actionUrl.Contains("/MyApplicationDetails"))
                {
                    // Try to extract job title from notification message
                    // Message format: "... Position: {JobTitle}" or "... for '{JobTitle}' at ..."
                    string? jobTitle = null;
                    if (!string.IsNullOrEmpty(notification.Message))
                    {
                        // Try "Position: {JobTitle}" format
                        var positionIndex = notification.Message.IndexOf("Position: ", StringComparison.OrdinalIgnoreCase);
                        if (positionIndex >= 0)
                        {
                            jobTitle = notification.Message.Substring(positionIndex + 10).Trim();
                        }
                        // Try "for '{JobTitle}'" format
                        else
                        {
                            var forIndex = notification.Message.IndexOf("for '", StringComparison.OrdinalIgnoreCase);
                            if (forIndex >= 0)
                            {
                                var startIndex = forIndex + 5;
                                var endIndex = notification.Message.IndexOf("'", startIndex);
                                if (endIndex > startIndex)
                                {
                                    jobTitle = notification.Message.Substring(startIndex, endIndex - startIndex);
                                }
                            }
                        }
                    }

                    // Find the application by job title if extracted, otherwise by time
                    int applicationId = 0;
                    if (!string.IsNullOrEmpty(jobTitle))
                    {
                        applicationId = await _context.JobApplications
                            .Where(a => a.JobSeekerProfile.UserId == userId)
                            .Where(a => a.JobVacancy.JobTitle == jobTitle)
                            .OrderByDescending(a => a.ApplyDate)
                            .Select(a => a.JobApplicationId)
                            .FirstOrDefaultAsync();
                    }

                    // Fallback: find by time proximity
                    if (applicationId == 0)
                    {
                        applicationId = await _context.JobApplications
                            .Where(a => a.JobSeekerProfile.UserId == userId)
                            .Where(a => a.ApplyDate <= notification.CreatedAt.AddMinutes(5))
                            .OrderByDescending(a => a.ApplyDate)
                            .Select(a => a.JobApplicationId)
                            .FirstOrDefaultAsync();
                    }

                    if (applicationId > 0)
                    {
                        return RedirectToAction("MyApplicationDetails", "JobApplications", new { id = applicationId });
                    }
                }

                return Redirect(actionUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: Notifications/MarkAsRead/{id}
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var success = await _notificationService.MarkAsReadAsync(id, userId.Value);
            if (success)
            {
                TempData["SuccessMessage"] = "Notification marked as read.";
            }
            else
            {
                TempData["ErrorMessage"] = "Notification not found.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: Notifications/MarkAllAsRead
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var count = await _notificationService.MarkAllAsReadAsync(userId.Value);
            TempData["SuccessMessage"] = $"{count} notification(s) marked as read.";

            return RedirectToAction(nameof(Index));
        }

        #region API Endpoints

        /// <summary>
        /// GET: /api/notifications/unread - Get unread notifications (API)
        /// </summary>
        [HttpGet("api/notifications/unread")]
        public async Task<IActionResult> GetUnreadApi([FromQuery] int limit = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            try
            {
                var notifications = await _notificationService.GetUnreadNotificationsAsync(userId.Value, limit);
                var count = await _notificationService.GetUnreadCountAsync(userId.Value);

                return Ok(new
                {
                    success = true,
                    notifications = notifications,
                    unreadCount = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// GET: /api/notifications/count - Get unread notification count (API)
        /// </summary>
        [HttpGet("api/notifications/count")]
        public async Task<IActionResult> GetUnreadCountApi()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            try
            {
                var count = await _notificationService.GetUnreadCountAsync(userId.Value);
                return Ok(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// POST: /api/notifications/mark-read/{id} - Mark notification as read (API)
        /// </summary>
        [HttpPost("api/notifications/mark-read/{id}")]
        public async Task<IActionResult> MarkAsReadApi(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            try
            {
                var success = await _notificationService.MarkAsReadAsync(id, userId.Value);
                if (success)
                {
                    var count = await _notificationService.GetUnreadCountAsync(userId.Value);
                    return Ok(new { success = true, unreadCount = count });
                }
                return BadRequest(new { success = false, message = "Notification not found or already read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// POST: /api/notifications/mark-all-read - Mark all notifications as read (API)
        /// </summary>
        [HttpPost("api/notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllAsReadApi()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            try
            {
                var count = await _notificationService.MarkAllAsReadAsync(userId.Value);
                return Ok(new { success = true, markedCount = count, unreadCount = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        #endregion

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;

            if (span.TotalDays >= 30)
                return $"{(int)(span.TotalDays / 30)} month(s) ago";
            if (span.TotalDays >= 7)
                return $"{(int)(span.TotalDays / 7)} week(s) ago";
            if (span.TotalDays >= 1)
                return $"{(int)span.TotalDays} day(s) ago";
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours} hour(s) ago";
            if (span.TotalMinutes >= 1)
                return $"{(int)span.TotalMinutes} minute(s) ago";
            return "Just now";
        }
    }
}
