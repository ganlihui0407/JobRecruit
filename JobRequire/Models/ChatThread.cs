using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a chat thread tied to a specific job application.
    /// Each JobApplication has at most ONE chat thread.
    /// </summary>
    public class ChatThread
    {
        [Key]
        public int ChatThreadId { get; set; }

        [Required]
        public int JobApplicationId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("JobApplicationId")]
        public JobApplication JobApplication { get; set; } = null!;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}







