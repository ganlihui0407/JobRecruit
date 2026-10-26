using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Stores history of application status changes with reasons.
    /// Each status update by an employer creates a new record.
    /// </summary>
    public class ApplicationStatusHistory
    {
        [Key]
        public int StatusHistoryId { get; set; }

        [Required]
        public int JobApplicationId { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int UpdatedByEmployerId { get; set; }

        // Navigation Properties
        [ForeignKey("JobApplicationId")]
        public JobApplication JobApplication { get; set; } = null!;

        [ForeignKey("UpdatedByEmployerId")]
        public User UpdatedByEmployer { get; set; } = null!;
    }
}


















