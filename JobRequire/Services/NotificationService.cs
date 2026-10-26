using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;

namespace JobRequire.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CreateNotificationAsync(int userId, string title, string? message = null,
            string type = "Info", string? actionUrl = null, string? actionText = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    ActionUrl = actionUrl,
                    ActionText = actionText,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notification created for user {userId}: {title}");

                return notification.NotificationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating notification for user {userId}");
                return 0;
            }
        }

        public async Task<int> CreateNotificationWithActivityAsync(int userId, string title, string? message = null,
            string type = "Info", string? actionUrl = null, string? actionText = null, string? activityAction = null, string? activityDetails = null)
        {
            // Create notification
            var notificationId = await CreateNotificationAsync(userId, title, message, type, actionUrl, actionText);

            // Create activity log
            if (!string.IsNullOrEmpty(activityAction))
            {
                var activityLog = new ActivityLog
                {
                    UserId = userId,
                    Action = activityAction,
                    Timestamp = DateTime.UtcNow,
                    Details = activityDetails ?? title
                };

                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();
            }

            return notificationId;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {notificationId} as read");
                return false;
            }
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return unreadNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking all notifications as read for user {userId}");
                return 0;
            }
        }

        public async Task<List<NotificationViewModel>> GetUnreadNotificationsAsync(int userId, int limit = 10)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(limit)
                    .Select(n => new NotificationViewModel
                    {
                        NotificationId = n.NotificationId,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        ActionUrl = n.ActionUrl,
                        ActionText = n.ActionText,
                        CreatedAt = n.CreatedAt,
                        TimeAgo = GetTimeAgo(n.CreatedAt)
                    })
                    .ToListAsync();

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unread notifications for user {userId}");
                return new List<NotificationViewModel>();
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unread count for user {userId}");
                return 0;
            }
        }

        public async Task<int> DeleteOldNotificationsAsync(int daysOld = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                var oldNotifications = await _context.Notifications
                    .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                    .ToListAsync();

                _context.Notifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();

                return oldNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old notifications");
                return 0;
            }
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

