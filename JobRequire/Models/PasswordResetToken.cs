using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a token for password reset functionality.
    /// Tokens are sent via email and have limited validity.
    /// </summary>
    public class PasswordResetToken
    {
        /// <summary>
        /// Primary key for the PasswordResetToken entity.
        /// </summary>
        [Key]
        public int PasswordResetTokenId { get; set; }

        /// <summary>
        /// Foreign key to the User requesting password reset.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Unique token string sent via password reset email.
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Expiration timestamp for the token (UTC).
        /// </summary>
        [Required]
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// Indicates if the token has been used. Used tokens cannot be reused.
        /// </summary>
        public bool IsUsed { get; set; } = false;

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The User this password reset token belongs to.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

