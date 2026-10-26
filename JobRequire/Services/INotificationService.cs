namespace JobRequire.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Create a notification for a user
        /// </summary>
        Task<int> CreateNotificationAsync(int userId, string title, string? message = null, 
            string type = "Info", string? actionUrl = null, string? actionText = null);

        /// <summary>
        /// Create a notification and also log to ActivityLogs
        /// </summary>
        Task<int> CreateNotificationWithActivityAsync(int userId, string title, string? message = null,
            string type = "Info", string? actionUrl = null, string? actionText = null, string? activityAction = null, string? activityDetails = null);

        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task<int> MarkAllAsReadAsync(int userId);

        /// <summary>
        /// Get unread notifications for a user
        /// </summary>
        Task<List<NotificationViewModel>> GetUnreadNotificationsAsync(int userId, int limit = 10);

        /// <summary>
        /// Get notification count for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(int userId);

        /// <summary>
        /// Delete old notifications (cleanup)
        /// </summary>
        Task<int> DeleteOldNotificationsAsync(int daysOld = 30);
    }

    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string Type { get; set; } = "Info";
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}

