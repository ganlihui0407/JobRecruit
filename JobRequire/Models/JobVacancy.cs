using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a job posting created by an Employer.
    /// Contains all details about the position including title, salary, requirements, and status.
    /// </summary>
    /// <remarks>
    /// Design Decision: JobVacancy is the central entity for job listings.
    /// - Links to EmployerProfile (owner of the job posting)
    /// - Has multiple JobApplications from JobSeekers
    /// - Can be bookmarked by JobSeekers for later viewing
    /// - Tracks interest count for popularity metrics
    /// </remarks>
    public class JobVacancy
    {
        /// <summary>
        /// Primary key for the JobVacancy entity.
        /// </summary>
        [Key]
        public int JobVacancyId { get; set; }

        /// <summary>
        /// Foreign key to the EmployerProfile that owns this job posting.
        /// </summary>
        [Required]
        public int EmployerProfileId { get; set; }

        /// <summary>
        /// Title of the job position (e.g., "Software Engineer", "Marketing Manager").
        /// </summary>
        [Required]
        [StringLength(150)]
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>
        /// Job category for filtering (e.g., "IT & Software", "Finance", "Marketing").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Employment type (e.g., "Full-Time", "Part-Time", "Contract", "Internship").
        /// </summary>
        [Required]
        [StringLength(50)]
        public string JobType { get; set; } = string.Empty;

        /// <summary>
        /// Minimum salary offered (in local currency). Null if negotiable.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal? SalaryMin { get; set; }

        /// <summary>
        /// Maximum salary offered (in local currency). Null if negotiable.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal? SalaryMax { get; set; }

        /// <summary>
        /// Work location (city, state, or "Remote").
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Detailed job description including responsibilities and benefits.
        /// </summary>
        [Required]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Job requirements including skills, qualifications, and experience.
        /// Used for resume-job matching feature.
        /// </summary>
        [StringLength(4000)]
        public string? Requirements { get; set; }

        /// <summary>
        /// Date when the job was published (UTC).
        /// </summary>
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Application deadline. Null if no deadline.
        /// </summary>
        public DateTime? ClosingDate { get; set; }

        /// <summary>
        /// Current status of the job posting.
        /// See <see cref="JobVacancyStatus"/> for valid values.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = JobVacancyStatus.Open;

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The employer who posted this job.
        /// </summary>
        [ForeignKey("EmployerProfileId")]
        public EmployerProfile EmployerProfile { get; set; } = null!;

        /// <summary>
        /// Collection of applications received for this job.
        /// </summary>
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();

        /// <summary>
        /// Collection of bookmarks from JobSeekers who saved this job.
        /// </summary>
        public ICollection<BookmarkedJob> BookmarkedJobs { get; set; } = new List<BookmarkedJob>();

        /// <summary>
        /// Collection of interest records tracking job popularity.
        /// </summary>
        public ICollection<JobInterest> JobInterests { get; set; } = new List<JobInterest>();
    }

    /// <summary>
    /// Constants for job vacancy status values.
    /// </summary>
    public static class JobVacancyStatus
    {
        /// <summary>Job is actively accepting applications.</summary>
        public const string Open = "Open";
        /// <summary>Job is no longer accepting applications.</summary>
        public const string Closed = "Closed";
        /// <summary>Job is saved but not yet published.</summary>
        public const string Draft = "Draft";
    }
}

