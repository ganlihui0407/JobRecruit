using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents an education entry in a JobSeeker's resume.
    /// Part of the website resume builder feature.
    /// </summary>
    public class ResumeEducation
    {
        /// <summary>
        /// Primary key for the ResumeEducation entity.
        /// </summary>
        [Key]
        public int ResumeEducationId { get; set; }

        /// <summary>
        /// Foreign key to the JobSeekerProfile that owns this education entry.
        /// </summary>
        [Required]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Name of the school, college, or university.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string SchoolName { get; set; } = string.Empty;

        /// <summary>
        /// Qualification obtained (e.g., "Bachelor's Degree", "Master's Degree", "Diploma").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Qualification { get; set; } = string.Empty;

        /// <summary>
        /// Field or major of study (e.g., "Computer Science", "Business Administration").
        /// </summary>
        [StringLength(150)]
        public string? FieldOfStudy { get; set; }

        /// <summary>
        /// Year when studies began.
        /// </summary>
        [Range(1950, 2100)]
        public int? StartYear { get; set; }

        /// <summary>
        /// Year when studies ended or expected to end. Null if currently studying.
        /// </summary>
        [Range(1950, 2100)]
        public int? EndYear { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The JobSeeker profile that owns this education entry.
        /// </summary>
        [ForeignKey("JobSeekerProfileId")]
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;
    }
}

