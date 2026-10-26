using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a work experience entry in a JobSeeker's resume.
    /// Part of the website resume builder feature.
    /// </summary>
    public class ResumeExperience
    {
        /// <summary>
        /// Primary key for the ResumeExperience entity.
        /// </summary>
        [Key]
        public int ResumeExperienceId { get; set; }

        /// <summary>
        /// Foreign key to the JobSeekerProfile that owns this experience entry.
        /// </summary>
        [Required]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Name of the company or organization.
        /// </summary>
        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Job title or position held.
        /// </summary>
        [Required]
        [StringLength(150)]
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>
        /// Date when employment started.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Date when employment ended. Null if currently employed.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Description of responsibilities, achievements, and duties.
        /// </summary>
        [StringLength(2000)]
        public string? Description { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The JobSeeker profile that owns this experience entry.
        /// </summary>
        [ForeignKey("JobSeekerProfileId")]
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;
    }
}

