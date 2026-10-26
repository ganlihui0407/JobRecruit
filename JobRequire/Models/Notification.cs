using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// In-app notifications for users
    /// </summary>
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Message { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Error

        [StringLength(200)]
        public string? ActionUrl { get; set; } // URL to navigate when clicked

        [StringLength(100)]
        public string? ActionText { get; set; } // Text for action button

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

