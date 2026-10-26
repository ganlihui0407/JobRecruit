using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Tracks failed login attempts for security purposes.
    /// Used to implement account lockout after multiple failures.
    /// </summary>
    public class FailedLoginAttempt
    {
        /// <summary>
        /// Primary key for the FailedLoginAttempt entity.
        /// </summary>
        [Key]
        public int FailedLoginAttemptId { get; set; }

        /// <summary>
        /// Foreign key to the User whose login attempts are being tracked.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Number of consecutive failed login attempts.
        /// Reset to 0 on successful login.
        /// </summary>
        [Required]
        [Range(0, int.MaxValue)]
        public int AttemptCount { get; set; } = 0;

        /// <summary>
        /// Timestamp of the last failed attempt (UTC).
        /// Used to determine lockout duration.
        /// </summary>
        public DateTime? LastAttemptTime { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The User whose login attempts are being tracked.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

