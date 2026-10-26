using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a token for email verification during account registration.
    /// Tokens have a 2-minute expiry and can only be used once.
    /// </summary>
    public class EmailVerificationToken
    {
        /// <summary>
        /// Primary key for the EmailVerificationToken entity.
        /// </summary>
        [Key]
        public int EmailVerificationTokenId { get; set; }

        /// <summary>
        /// Foreign key to the User who needs email verification.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Unique token string sent via email activation link.
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
        /// The User this verification token belongs to.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

