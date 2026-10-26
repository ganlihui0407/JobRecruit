using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Service for managing application chat functionality.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatService> _logger;

        // Statuses that make chat unavailable
        private static readonly string[] UnavailableStatuses = { "Pending" };
        
        // Statuses that make chat read-only
        private static readonly string[] ReadOnlyStatuses = { "Rejected", "Withdrawn" };

        public ChatService(ApplicationDbContext context, ILogger<ChatService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets the sender role that would be considered "the other party" for unread counting.
        /// </summary>
        private static string GetOtherSenderRole(string userRole)
        {
            return userRole switch
            {
                "Employer" => ChatSenderRole.JobSeeker,
                "JobSeeker" => ChatSenderRole.Employer,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets the sender role for the current user.
        /// </summary>
        private static string GetSenderRoleForUser(string userRole)
        {
            return userRole switch
            {
                "Employer" => ChatSenderRole.Employer,
                "JobSeeker" => ChatSenderRole.JobSeeker,
                "Admin" => ChatSenderRole.System,
                _ => ChatSenderRole.System
            };
        }

        public async Task<bool> IsChatAvailableAsync(int jobApplicationId)
        {
            var status = await GetApplicationStatusAsync(jobApplicationId);
            if (string.IsNullOrEmpty(status))
                return false;

            return !UnavailableStatuses.Contains(status);
        }

        public async Task<bool> IsChatReadOnlyAsync(int jobApplicationId)
        {
            var status = await GetApplicationStatusAsync(jobApplicationId);
            if (string.IsNullOrEmpty(status))
                return true;

            return ReadOnlyStatuses.Contains(status);
        }

        public async Task<string?> GetApplicationStatusAsync(int jobApplicationId)
        {
            return await _context.JobApplications
                .Where(ja => ja.JobApplicationId == jobApplicationId)
                .Select(ja => ja.Status)
                .FirstOrDefaultAsync();
        }

        public async Task<ChatThread?> GetOrCreateChatThreadAsync(int jobApplicationId)
        {
            // Check if chat is available
            if (!await IsChatAvailableAsync(jobApplicationId))
            {
                return null;
            }

            // Try to get existing thread
            var thread = await _context.ChatThreads
                .Include(ct => ct.Messages)
                .FirstOrDefaultAsync(ct => ct.JobApplicationId == jobApplicationId);

            if (thread != null)
            {
                return thread;
            }

            // Create new thread with system message
            thread = new ChatThread
            {
                JobApplicationId = jobApplicationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatThreads.Add(thread);
            await _context.SaveChangesAsync();

            // Add system message
            var systemMessage = new ChatMessage
            {
                ChatThreadId = thread.ChatThreadId,
                SenderRole = ChatSenderRole.System,
                Message = "Your application status has been updated. You may communicate here regarding this application.",
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(systemMessage);
            await _context.SaveChangesAsync();

            // Reload with messages
            thread = await _context.ChatThreads
                .Include(ct => ct.Messages)
                .FirstOrDefaultAsync(ct => ct.ChatThreadId == thread.ChatThreadId);

            _logger.LogInformation("Created chat thread {ChatThreadId} for application {JobApplicationId}", 
                thread?.ChatThreadId, jobApplicationId);

            return thread;
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(int jobApplicationId)
        {
            var thread = await _context.ChatThreads
                .Include(ct => ct.Messages)
                .FirstOrDefaultAsync(ct => ct.JobApplicationId == jobApplicationId);

            if (thread == null)
            {
                return new List<ChatMessage>();
            }

            return thread.Messages
                .OrderBy(m => m.CreatedAt)
                .ToList();
        }

        public async Task<ChatMessage?> SendMessageAsync(int jobApplicationId, string senderRole, string message)
        {
            // Validate chat is available and not read-only
            if (!await IsChatAvailableAsync(jobApplicationId))
            {
                _logger.LogWarning("Attempted to send message to unavailable chat for application {JobApplicationId}", 
                    jobApplicationId);
                return null;
            }

            if (await IsChatReadOnlyAsync(jobApplicationId))
            {
                _logger.LogWarning("Attempted to send message to read-only chat for application {JobApplicationId}", 
                    jobApplicationId);
                return null;
            }

            // Get or create thread
            var thread = await GetOrCreateChatThreadAsync(jobApplicationId);
            if (thread == null)
            {
                return null;
            }

            // Get the receiver's UserId (the opposite party)
            var application = await _context.JobApplications
                .Include(ja => ja.JobSeekerProfile)
                .Include(ja => ja.JobVacancy)
                    .ThenInclude(jv => jv.EmployerProfile)
                .FirstOrDefaultAsync(ja => ja.JobApplicationId == jobApplicationId);

            int? receiverId = null;
            if (application != null)
            {
                // If sender is Employer, receiver is JobSeeker and vice versa
                if (senderRole == ChatSenderRole.Employer)
                {
                    receiverId = application.JobSeekerProfile.UserId;
                }
                else if (senderRole == ChatSenderRole.JobSeeker)
                {
                    receiverId = application.JobVacancy?.EmployerProfile?.UserId;
                }
            }

            // Create message - IsRead = false for receiver (they haven't seen it yet)
            var chatMessage = new ChatMessage
            {
                ChatThreadId = thread.ChatThreadId,
                SenderRole = senderRole,
                Message = message.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsRead = false, // Receiver hasn't read it yet
                ReceiverId = receiverId
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message sent in chat thread {ChatThreadId} by {SenderRole} to receiver {ReceiverId}", 
                thread.ChatThreadId, senderRole, receiverId);

            return chatMessage;
        }

        public async Task<bool> ValidateUserAccessAsync(int jobApplicationId, int userId, string userRole)
        {
            var application = await _context.JobApplications
                .Include(ja => ja.JobSeekerProfile)
                .Include(ja => ja.JobVacancy)
                    .ThenInclude(jv => jv.EmployerProfile)
                .FirstOrDefaultAsync(ja => ja.JobApplicationId == jobApplicationId);

            if (application == null)
            {
                return false;
            }

            // Admin has access to all
            if (userRole == "Admin")
            {
                return true;
            }

            // JobSeeker can only access their own applications
            if (userRole == "JobSeeker")
            {
                return application.JobSeekerProfile.UserId == userId;
            }

            // Employer can only access applications for their job vacancies
            if (userRole == "Employer")
            {
                return application.JobVacancy?.EmployerProfile?.UserId == userId;
            }

            return false;
        }

        public async Task<int> GetUnreadCountAsync(int userId, string userRole)
        {
            // Count all unread messages where the current user is the receiver
            var unreadCount = await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();

            // Also count legacy messages (without ReceiverId) using the old logic
            var otherSenderRole = GetOtherSenderRole(userRole);
            if (!string.IsNullOrEmpty(otherSenderRole))
            {
                // Get all chat threads the user has access to
                var query = _context.ChatThreads
                    .Include(ct => ct.JobApplication)
                        .ThenInclude(ja => ja.JobSeekerProfile)
                    .Include(ct => ct.JobApplication)
                        .ThenInclude(ja => ja.JobVacancy)
                            .ThenInclude(jv => jv.EmployerProfile)
                    .Include(ct => ct.Messages)
                    .AsQueryable();

                // Filter based on user role
                if (userRole == "JobSeeker")
                {
                    query = query.Where(ct => ct.JobApplication.JobSeekerProfile.UserId == userId);
                }
                else if (userRole == "Employer")
                {
                    query = query.Where(ct => ct.JobApplication.JobVacancy.EmployerProfile.UserId == userId);
                }

                // Count legacy unread messages (where ReceiverId is null)
                var threads = await query.ToListAsync();
                var legacyUnreadCount = threads
                    .SelectMany(ct => ct.Messages)
                    .Count(m => !m.IsRead && m.ReceiverId == null && m.SenderRole == otherSenderRole);

                unreadCount += legacyUnreadCount;
            }

            return unreadCount;
        }

        public async Task MarkThreadAsReadAsync(int jobApplicationId, string userRole)
        {
            // Get the current user's ID based on the application and role
            var application = await _context.JobApplications
                .Include(ja => ja.JobSeekerProfile)
                .Include(ja => ja.JobVacancy)
                    .ThenInclude(jv => jv.EmployerProfile)
                .FirstOrDefaultAsync(ja => ja.JobApplicationId == jobApplicationId);

            if (application == null)
            {
                return;
            }

            int? currentUserId = userRole switch
            {
                "JobSeeker" => application.JobSeekerProfile.UserId,
                "Employer" => application.JobVacancy?.EmployerProfile?.UserId,
                _ => null
            };

            var thread = await _context.ChatThreads
                .Include(ct => ct.Messages)
                .FirstOrDefaultAsync(ct => ct.JobApplicationId == jobApplicationId);

            if (thread == null)
            {
                return;
            }

            var otherSenderRole = GetOtherSenderRole(userRole);
            var now = DateTime.UtcNow;

            // Mark messages as read where:
            // 1. ReceiverId matches current user, OR
            // 2. Legacy messages (ReceiverId is null) from the other party
            var unreadMessages = thread.Messages
                .Where(m => !m.IsRead && 
                    ((m.ReceiverId != null && m.ReceiverId == currentUserId) ||
                     (m.ReceiverId == null && !string.IsNullOrEmpty(otherSenderRole) && m.SenderRole == otherSenderRole)))
                .ToList();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = now;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} messages as read in thread for application {JobApplicationId}", 
                    unreadMessages.Count, jobApplicationId);
            }
        }

        public async Task<List<ChatThreadSummary>> GetChatInboxAsync(int userId, string userRole)
        {
            var otherSenderRole = GetOtherSenderRole(userRole);
            
            var query = _context.ChatThreads
                .Include(ct => ct.JobApplication)
                    .ThenInclude(ja => ja.JobSeekerProfile)
                .Include(ct => ct.JobApplication)
                    .ThenInclude(ja => ja.JobVacancy)
                        .ThenInclude(jv => jv.EmployerProfile)
                .Include(ct => ct.Messages)
                .AsQueryable();

            // Filter based on user role
            if (userRole == "JobSeeker")
            {
                query = query.Where(ct => ct.JobApplication.JobSeekerProfile.UserId == userId);
            }
            else if (userRole == "Employer")
            {
                query = query.Where(ct => ct.JobApplication.JobVacancy.EmployerProfile.UserId == userId);
            }
            else
            {
                return new List<ChatThreadSummary>();
            }

            var threads = await query.ToListAsync();

            var summaries = threads.Select(ct => {
                var lastMessage = ct.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                
                // Count unread messages where ReceiverId matches current user
                // Also count legacy messages (ReceiverId is null) from the other party
                var unreadCount = ct.Messages.Count(m => !m.IsRead && 
                    ((m.ReceiverId != null && m.ReceiverId == userId) ||
                     (m.ReceiverId == null && !string.IsNullOrEmpty(otherSenderRole) && m.SenderRole == otherSenderRole)));

                return new ChatThreadSummary
                {
                    ChatThreadId = ct.ChatThreadId,
                    JobApplicationId = ct.JobApplicationId,
                    JobTitle = ct.JobApplication.JobVacancy?.JobTitle ?? "Unknown Job",
                    CompanyName = ct.JobApplication.JobVacancy?.EmployerProfile?.CompanyName ?? "Unknown Company",
                    LastMessage = lastMessage?.Message,
                    LastMessageTime = lastMessage?.CreatedAt,
                    UnreadCount = unreadCount,
                    ApplicationStatus = ct.JobApplication.Status
                };
            })
            .OrderByDescending(s => s.LastMessageTime ?? DateTime.MinValue)
            .ToList();

            return summaries;
        }
    }
}


