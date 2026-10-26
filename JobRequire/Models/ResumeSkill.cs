using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a skill entry in a JobSeeker's resume.
    /// Part of the website resume builder feature.
    /// Used for resume-job matching calculations.
    /// </summary>
    public class ResumeSkill
    {
        /// <summary>
        /// Primary key for the ResumeSkill entity.
        /// </summary>
        [Key]
        public int ResumeSkillId { get; set; }

        /// <summary>
        /// Foreign key to the JobSeekerProfile that owns this skill entry.
        /// </summary>
        [Required]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Name of the skill (e.g., "Python", "Project Management", "SQL").
        /// Used for matching against job requirements.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SkillName { get; set; } = string.Empty;

        /// <summary>
        /// Proficiency level. See <see cref="SkillLevelType"/> for valid values.
        /// </summary>
        [StringLength(50)]
        public string? SkillLevel { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The JobSeeker profile that owns this skill entry.
        /// </summary>
        [ForeignKey("JobSeekerProfileId")]
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;
    }

    /// <summary>
    /// Constants for skill proficiency levels.
    /// </summary>
    public static class SkillLevelType
    {
        public const string Beginner = "Beginner";
        public const string Intermediate = "Intermediate";
        public const string Advanced = "Advanced";
        public const string Expert = "Expert";
    }
}

