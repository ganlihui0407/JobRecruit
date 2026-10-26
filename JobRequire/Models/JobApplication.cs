using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a job application submitted by a JobSeeker for a specific JobVacancy.
    /// Tracks the application lifecycle from submission through hiring or rejection.
    /// </summary>
    /// <remarks>
    /// Design Decision: JobApplication is the central entity connecting JobSeekers to Jobs.
    /// - Contains a snapshot of the resume at application time (immutable record)
    /// - Supports both PDF upload and website resume sources
    /// - Has a 1:1 relationship with ChatThread for employer-applicant communication
    /// - Tracks full status history with reasons for each change
    /// </remarks>
    public class JobApplication
    {
        /// <summary>
        /// Primary key for the JobApplication entity.
        /// </summary>
        [Key]
        public int JobApplicationId { get; set; }

        /// <summary>
        /// Foreign key to the JobSeeker who submitted this application.
        /// </summary>
        [Required]
        public int JobSeekerProfileId { get; set; }

        /// <summary>
        /// Foreign key to the JobVacancy being applied for.
        /// </summary>
        [Required]
        public int JobVacancyId { get; set; }

        /// <summary>
        /// Timestamp when the application was submitted (UTC).
        /// </summary>
        public DateTime ApplyDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Current status of the application.
        /// See <see cref="ApplicationStatus"/> for valid values.
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = ApplicationStatus.Pending;

        /// <summary>
        /// JSON snapshot of the resume at time of application.
        /// This preserves the resume state even if the JobSeeker updates their profile later.
        /// </summary>
        [StringLength(4000)]
        public string? ResumeSnapshot { get; set; }

        /// <summary>
        /// Legacy field for additional document URL. Use ApplicationDocuments collection instead.
        /// </summary>
        [StringLength(500)]
        public string? AdditionalDocument { get; set; }

        /// <summary>
        /// Indicates how the resume was submitted.
        /// See <see cref="ResumeSourceType"/> for valid values.
        /// </summary>
        [StringLength(20)]
        public string ResumeSource { get; set; } = ResumeSourceType.Website;

        /// <summary>
        /// File path to uploaded PDF resume (if ResumeSource is "PDF").
        /// </summary>
        [StringLength(500)]
        public string? ResumeFilePath { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The JobSeeker who submitted this application.
        /// </summary>
        [ForeignKey("JobSeekerProfileId")]
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;

        /// <summary>
        /// The job being applied for.
        /// </summary>
        [ForeignKey("JobVacancyId")]
        public JobVacancy JobVacancy { get; set; } = null!;

        /// <summary>
        /// Collection of additional documents attached to this application.
        /// </summary>
        public ICollection<ApplicationDocument> ApplicationDocuments { get; set; } = new List<ApplicationDocument>();

        /// <summary>
        /// Collection of status change records with timestamps and reasons.
        /// </summary>
        public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();

        /// <summary>
        /// Collection of interview details when application reaches Interview status.
        /// </summary>
        public ICollection<InterviewDetails> InterviewDetails { get; set; } = new List<InterviewDetails>();

        /// <summary>
        /// Collection of offer details when application reaches Offered status.
        /// </summary>
        public ICollection<OfferDetails> OfferDetails { get; set; } = new List<OfferDetails>();

        /// <summary>
        /// Chat thread for communication between employer and applicant.
        /// One-to-one relationship: each application has at most one chat thread.
        /// </summary>
        public ChatThread? ChatThread { get; set; }
    }

    /// <summary>
    /// Constants for resume source types.
    /// </summary>
    public static class ResumeSourceType
    {
        /// <summary>Resume was uploaded as a PDF file.</summary>
        public const string PDF = "PDF";
        /// <summary>Resume was built using the website resume builder.</summary>
        public const string Website = "Website";
    }

    /// <summary>
    /// Constants for application status values.
    /// </summary>
    public static class ApplicationStatus
    {
        /// <summary>Application submitted, awaiting review.</summary>
        public const string Pending = "Pending";
        /// <summary>Employer has viewed the application.</summary>
        public const string Reviewed = "Reviewed";
        /// <summary>Applicant has been shortlisted for consideration.</summary>
        public const string Shortlisted = "Shortlisted";
        /// <summary>Applicant has been scheduled for an interview.</summary>
        public const string Interview = "Interview";
        /// <summary>Job offer has been extended to the applicant.</summary>
        public const string Offered = "Offered";
        /// <summary>Applicant has been hired.</summary>
        public const string Hired = "Hired";
        /// <summary>Application has been rejected.</summary>
        public const string Rejected = "Rejected";
        /// <summary>Applicant withdrew their application.</summary>
        public const string Withdrawn = "Withdrawn";
    }
}

