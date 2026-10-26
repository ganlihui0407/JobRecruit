using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a review written by a JobSeeker for an EmployerProfile (company).
    /// </summary>
    public class CompanyReview
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int EmployerProfileId { get; set; }

        [Required]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Rating from 1 to 5 stars.
        /// </summary>
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        /// <summary>
        /// Review comment text.
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the review.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = ReviewStatus.Active;

        /// <summary>
        /// When the review was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the review was reported by the employer (UTC). Null if not reported.
        /// </summary>
        public DateTime? ReportedAt { get; set; }

        /// <summary>
        /// Whether the JobSeeker has responded to a review report.
        /// If true, the review will NOT be auto-hidden after 3 days.
        /// </summary>
        public bool JobseekerResponded { get; set; } = false;

        /// <summary>
        /// The JobSeeker's response to the employer's report.
        /// </summary>
        [StringLength(1000)]
        public string? ResponseComment { get; set; }

        /// <summary>
        /// When the JobSeeker responded to the report (UTC).
        /// </summary>
        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        [ForeignKey("EmployerProfileId")]
        public EmployerProfile EmployerProfile { get; set; } = null!;

        [ForeignKey("JobSeekerProfileId")]
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;
    }

    /// <summary>
    /// Constants for review status values.
    /// </summary>
    public static class ReviewStatus
    {
        /// <summary>
        /// Review is active and visible to all users.
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// Review has been reported by the employer and is under review.
        /// Still visible but with a warning label.
        /// </summary>
        public const string UnderReview = "UnderReview";

        /// <summary>
        /// Review has been hidden (auto-hidden after 3 days if JobSeeker didn't respond).
        /// Not visible to public users.
        /// </summary>
        public const string Hidden = "Hidden";
    }
}



