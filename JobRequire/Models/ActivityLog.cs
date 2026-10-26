using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents an audit log entry tracking user actions in the system.
    /// Used for security monitoring and activity tracking.
    /// </summary>
    public class ActivityLog
    {
        /// <summary>
        /// Primary key for the ActivityLog entity.
        /// </summary>
        [Key]
        public int ActivityLogId { get; set; }

        /// <summary>
        /// Foreign key to the User who performed the action.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Type of action performed (e.g., "Login", "ProfileUpdate", "JobPost").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the action occurred (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional details about the action (JSON or plain text).
        /// </summary>
        [StringLength(1000)]
        public string? Details { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The User who performed this action.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

