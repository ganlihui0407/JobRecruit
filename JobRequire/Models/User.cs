using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a user account in the system.
    /// This is the core authentication entity that links to role-specific profiles.
    /// Users can be Admins, Employers, or JobSeekers.
    /// </summary>
    /// <remarks>
    /// Design Decision: User is separate from profile entities to support:
    /// - Single authentication mechanism across all roles
    /// - Role-based access control via the Role property
    /// - Future support for users with multiple roles
    /// </remarks>
    public class User
    {
        /// <summary>
        /// Primary key for the User entity.
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// Unique username for login. Must be alphanumeric with underscores only.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User's email address. Used for login and notifications.
        /// </summary>
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password using BCrypt. Never store plain text passwords.
        /// </summary>
        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's role in the system. Valid values: "Admin", "Employer", "JobSeeker".
        /// See <see cref="UserRoles"/> for constants.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the account is active. Inactive accounts cannot log in.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Timestamp when the account was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// Employer profile if user is an Employer. Null for other roles.
        /// </summary>
        public EmployerProfile? EmployerProfile { get; set; }

        /// <summary>
        /// JobSeeker profile if user is a JobSeeker. Null for other roles.
        /// </summary>
        public JobSeekerProfile? JobSeekerProfile { get; set; }

        /// <summary>
        /// Collection of failed login attempts for security tracking.
        /// </summary>
        public ICollection<FailedLoginAttempt> FailedLoginAttempts { get; set; } = new List<FailedLoginAttempt>();

        /// <summary>
        /// Collection of email verification tokens for account activation.
        /// </summary>
        public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

        /// <summary>
        /// Collection of password reset tokens for password recovery.
        /// </summary>
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

        /// <summary>
        /// Collection of activity logs tracking user actions.
        /// </summary>
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

        /// <summary>
        /// Collection of in-app notifications for this user.
        /// </summary>
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        /// <summary>
        /// Collection of application status updates made by this user (Employer only).
        /// </summary>
        public ICollection<ApplicationStatusHistory> StatusHistoryUpdates { get; set; } = new List<ApplicationStatusHistory>();
    }

    /// <summary>
    /// Constants for user role values.
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Employer = "Employer";
        public const string JobSeeker = "JobSeeker";
    }
}
