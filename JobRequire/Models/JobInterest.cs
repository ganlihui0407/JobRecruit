using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a JobSeeker's interest in a job vacancy.
    /// Used to track job popularity metrics (e.g., "🔥 128 jobseekers interested").
    /// Many-to-many relationship between JobSeekerProfile and JobVacancy.
    /// </summary>
    /// <remarks>
    /// Design Decision: Separate from BookmarkedJob to distinguish:
    /// - Bookmark = save for later (private action)
    /// - Interest = express interest (public popularity metric)
    /// A job is considered "Popular" when InterestedCount >= 50.
    /// </remarks>
    public class JobInterest
    {
        /// <summary>
        /// Primary key for the JobInterest entity.
        /// </summary>
        [Key]
        public int JobInterestId { get; set; }

        /// <summary>
        /// Foreign key to the job that received interest.
        /// </summary>
        [Required]
        public int JobVacancyId { get; set; }

        /// <summary>
        /// Foreign key to the JobSeeker who expressed interest.
        /// </summary>
        [Required]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Timestamp when interest was expressed (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The job that received interest.
        /// </summary>
        [ForeignKey("JobVacancyId")]
        public JobVacancy JobVacancy { get; set; } = null!;

        /// <summary>
        /// The JobSeeker who expressed interest.
        /// </summary>
        [ForeignKey("JobSeekerProfileId")]
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;
    }
}




















