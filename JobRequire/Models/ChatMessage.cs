using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a single message in a chat thread.
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int ChatMessageId { get; set; }

        [Required]
        public int ChatThreadId { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderRole { get; set; } = null!; // "System", "Employer", "JobSeeker"

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates whether the message has been read by the receiver.
        /// Sender side: IsRead = true, Receiver side: IsRead = false initially.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// The timestamp when the message was read by the receiver.
        /// Null if not yet read.
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// The UserId of the intended receiver of this message.
        /// Used for accurate unread message counting.
        /// </summary>
        public int? ReceiverId { get; set; }

        // Navigation Properties
        [ForeignKey("ChatThreadId")]
        public ChatThread ChatThread { get; set; } = null!;
    }

    /// <summary>
    /// Constants for chat sender roles
    /// </summary>
    public static class ChatSenderRole
    {
        public const string System = "System";
        public const string Employer = "Employer";
        public const string JobSeeker = "JobSeeker";
    }
}


