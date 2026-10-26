using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Stores interview details when an application status is set to "Interview".
    /// </summary>
    public class InterviewDetails
    {
        [Key]
        public int InterviewId { get; set; }

        [Required]
        public int JobApplicationId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime InterviewDate { get; set; }

        [Required]
        [StringLength(10)]
        public string InterviewTime { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string LocationOrLink { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? PdfPath { get; set; }

        // Navigation Property
        [ForeignKey("JobApplicationId")]
        public JobApplication JobApplication { get; set; } = null!;
    }
}


