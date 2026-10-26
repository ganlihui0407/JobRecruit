using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a job saved/bookmarked by a JobSeeker for later viewing.
    /// Many-to-many relationship between JobSeekerProfile and JobVacancy.
    /// </summary>
    public class BookmarkedJob
    {
        /// <summary>
        /// Primary key for the BookmarkedJob entity.
        /// </summary>
        [Key]
        public int BookmarkedJobId { get; set; }

        /// <summary>
        /// Foreign key to the JobSeeker who bookmarked the job.
        /// </summary>
        [Required]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Foreign key to the bookmarked job.
        /// </summary>
        [Required]
        public int JobVacancyId { get; set; }

        /// <summary>
        /// Timestamp when the job was bookmarked (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The JobSeeker who bookmarked this job.
        /// </summary>
        [ForeignKey("JobSeekerProfileId")]
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;

        /// <summary>
        /// The job that was bookmarked.
        /// </summary>
        [ForeignKey("JobVacancyId")]
        public JobVacancy JobVacancy { get; set; } = null!;
    }
}

