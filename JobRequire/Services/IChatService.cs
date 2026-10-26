using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Service interface for managing application chat functionality.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Checks if chat is available for the given application.
        /// Chat is available when status is NOT "Pending".
        /// </summary>
        Task<bool> IsChatAvailableAsync(int jobApplicationId);

        /// <summary>
        /// Checks if chat is read-only (status is "Rejected" or "Withdrawn").
        /// </summary>
        Task<bool> IsChatReadOnlyAsync(int jobApplicationId);

        /// <summary>
        /// Gets or creates a chat thread for the application.
        /// Creates a system message if this is a new thread.
        /// </summary>
        Task<ChatThread?> GetOrCreateChatThreadAsync(int jobApplicationId);

        /// <summary>
        /// Gets all messages for a job application.
        /// </summary>
        Task<List<ChatMessage>> GetMessagesAsync(int jobApplicationId);

        /// <summary>
        /// Sends a message in the chat thread.
        /// Returns null if chat is not available or is read-only.
        /// </summary>
        Task<ChatMessage?> SendMessageAsync(int jobApplicationId, string senderRole, string message);

        /// <summary>
        /// Validates if the user has access to the chat for the given application.
        /// </summary>
        Task<bool> ValidateUserAccessAsync(int jobApplicationId, int userId, string userRole);

        /// <summary>
        /// Gets the application status.
        /// </summary>
        Task<string?> GetApplicationStatusAsync(int jobApplicationId);

        /// <summary>
        /// Gets the total count of unread messages for a user.
        /// </summary>
        Task<int> GetUnreadCountAsync(int userId, string userRole);

        /// <summary>
        /// Marks all messages in a chat thread as read for the current user.
        /// </summary>
        Task MarkThreadAsReadAsync(int jobApplicationId, string userRole);

        /// <summary>
        /// Gets chat threads with summary info for inbox display.
        /// </summary>
        Task<List<ChatThreadSummary>> GetChatInboxAsync(int userId, string userRole);
    }

    /// <summary>
    /// Summary information for a chat thread in the inbox.
    /// </summary>
    public class ChatThreadSummary
    {
        public int ChatThreadId { get; set; }
        public int JobApplicationId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;
    }
}


