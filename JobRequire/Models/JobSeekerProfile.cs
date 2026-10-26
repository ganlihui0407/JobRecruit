using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a profile for users with the JobSeeker role.
    /// Contains personal information and links to resume components.
    /// </summary>
    /// <remarks>
    /// Design Decision: JobSeekerProfile is the hub for all job seeker data:
    /// - Personal info (name, phone, photo, summary)
    /// - Resume components (education, experience, skills) via navigation properties
    /// - Job search activity (applications, bookmarks, interests)
    /// 
    /// The resume is split into separate entities for flexibility and normalization.
    /// </remarks>
    public class JobSeekerProfile
    {
        /// <summary>
        /// Primary key for the JobSeekerProfile entity.
        /// </summary>
        [Key]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Foreign key to the User account that owns this profile.
        /// One-to-one relationship with User.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Full legal name of the job seeker.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Contact phone number.
        /// </summary>
        [StringLength(20)]
        [Phone]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Professional summary or career objective.
        /// </summary>
        [StringLength(2000)]
        public string? Summary { get; set; }

        /// <summary>
        /// File path or URL to the profile photo.
        /// </summary>
        [StringLength(500)]
        public string? ProfilePhoto { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The User account associated with this profile.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        /// <summary>
        /// Collection of job applications submitted by this job seeker.
        /// </summary>
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();

        /// <summary>
        /// Collection of education entries in the resume.
        /// </summary>
        public ICollection<ResumeEducation> ResumeEducations { get; set; } = new List<ResumeEducation>();

        /// <summary>
        /// Collection of work experience entries in the resume.
        /// </summary>
        public ICollection<ResumeExperience> ResumeExperiences { get; set; } = new List<ResumeExperience>();

        /// <summary>
        /// Collection of skills in the resume.
        /// </summary>
        public ICollection<ResumeSkill> ResumeSkills { get; set; } = new List<ResumeSkill>();

        /// <summary>
        /// Collection of jobs bookmarked by this job seeker.
        /// </summary>
        public ICollection<BookmarkedJob> BookmarkedJobs { get; set; } = new List<BookmarkedJob>();

        /// <summary>
        /// Collection of job interest records (for popularity tracking).
        /// </summary>
        public ICollection<JobInterest> JobInterests { get; set; } = new List<JobInterest>();
    }
}

