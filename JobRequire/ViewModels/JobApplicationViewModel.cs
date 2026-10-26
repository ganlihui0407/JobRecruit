using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using JobRequire.Models;

namespace JobRequire.ViewModels
{
    // =============================================
    // JOB APPLICATION DETAIL VIEW MODELS
    // =============================================
    // Purpose: ViewModels for viewing and managing job applications.
    // Includes: Application details, List view, Timeline, Chat.
    // 
    // Design Decision: Separate ViewModels for Employer and JobSeeker views.
    // Timeline shows full application history with interview/offer details.
    // =============================================

    /// <summary>
    /// ViewModel for displaying job application details to Employers.
    /// Includes applicant info, resume, documents, and status management.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Employer/ApplicationDetails/{id}
    /// Employers can update status and view applicant resume.
    /// </remarks>
    public class JobApplicationViewModel
    {
        public int JobApplicationId { get; set; }
        public int JobSeekerProfileId { get; set; }
        public int JobVacancyId { get; set; }

        [Display(Name = "Apply Date")]
        [DataType(DataType.DateTime)]
        public DateTime ApplyDate { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Resume Snapshot")]
        public string? ResumeSnapshot { get; set; }

        [Display(Name = "Additional Document")]
        public string? AdditionalDocument { get; set; }

        // Resume submission fields
        [Display(Name = "Resume Source")]
        public string ResumeSource { get; set; } = ResumeSourceType.Website;

        [Display(Name = "Resume File Path")]
        public string? ResumeFilePath { get; set; }

        // Job Seeker Info
        public int ApplicantUserId { get; set; }
        public string? ApplicantName { get; set; }
        public string? ApplicantEmail { get; set; }
        public string? ApplicantPhone { get; set; }
        public string? ApplicantPhoto { get; set; }
        public string? ApplicantSummary { get; set; }

        // Job Info
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? JobLocation { get; set; }

        // Resume sections
        public List<ResumeEducationViewModel> Education { get; set; } = new();
        public List<ResumeExperienceViewModel> Experience { get; set; } = new();
        public List<ResumeSkillViewModel> Skills { get; set; } = new();

        // Documents
        public List<ApplicationDocumentViewModel> Documents { get; set; } = new();

        // Status options
        public List<SelectListItem> StatusOptions { get; set; } = new();
        
        // Timeline data (for Employer view)
        public List<ApplicationTimelineItemViewModel> Timeline { get; set; } = new();

        // Helper properties for resume display
        public bool IsPdfResume => ResumeSource == ResumeSourceType.PDF;
        public bool IsWebsiteResume => ResumeSource == ResumeSourceType.Website;
        public bool HasResume => IsPdfResume ? !string.IsNullOrEmpty(ResumeFilePath) : true;
    }

    /// <summary>
    /// ViewModel for paginated list of job applications.
    /// Used by Employers to view all applications with filters and sorting.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Employer/Applications
    /// Supports filtering by job, status, date range, and search term.
    /// </remarks>
    public class JobApplicationListViewModel
    {
        public List<JobApplicationViewModel> Applications { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Filters
        public int? JobVacancyId { get; set; }
        public string? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Sorting
        public string SortBy { get; set; } = "ApplyDate";
        public string SortOrder { get; set; } = "desc";

        // Filter options
        public List<SelectListItem> JobVacancies { get; set; } = new();
        public List<SelectListItem> Statuses { get; set; } = new();

        // Job info for filtered view
        public string? CurrentJobTitle { get; set; }

        // Pagination helpers
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    // ApplicationDocumentViewModel is defined in FileUploadViewModels.cs

    /// <summary>
    /// ViewModel for Jobseeker's "View Application Details" page.
    /// Shows application details from the jobseeker's perspective.
    /// </summary>
    public class JobSeekerApplicationDetailsViewModel
    {
        public int JobApplicationId { get; set; }
        
        // Job Info
        public int JobVacancyId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string? Location { get; set; }
        public string? JobType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        
        // Application Info
        public string Status { get; set; } = "Pending";
        public DateTime ApplyDate { get; set; }
        
        // Status History - Latest reason from employer
        public string? LatestStatusReason { get; set; }
        public DateTime? LatestStatusUpdatedAt { get; set; }
        
        // Resume Info
        public string ResumeSource { get; set; } = ResumeSourceType.Website;
        public string? ResumeFilePath { get; set; }
        public string? ResumeSnapshot { get; set; }
        
        // Resume sections (for website resume preview)
        public List<ResumeEducationViewModel> Education { get; set; } = new();
        public List<ResumeExperienceViewModel> Experience { get; set; } = new();
        public List<ResumeSkillViewModel> Skills { get; set; } = new();
        
        // Applicant Info (from profile at time of viewing)
        public string? ApplicantName { get; set; }
        public string? ApplicantEmail { get; set; }
        public string? ApplicantPhone { get; set; }
        public string? ApplicantSummary { get; set; }
        
        // Documents
        public List<ApplicationDocumentViewModel> Documents { get; set; } = new();
        
        // Timeline data
        public List<ApplicationTimelineItemViewModel> Timeline { get; set; } = new();
        
        // Helper properties
        public bool IsPdfResume => ResumeSource == ResumeSourceType.PDF;
        public bool IsWebsiteResume => ResumeSource == ResumeSourceType.Website;
        public bool HasResume => IsPdfResume ? !string.IsNullOrEmpty(ResumeFilePath) : true;
        
        public string SalaryDisplay
        {
            get
            {
                if (SalaryMin.HasValue && SalaryMax.HasValue)
                    return $"RM {SalaryMin:N0} - RM {SalaryMax:N0}";
                if (SalaryMin.HasValue)
                    return $"From RM {SalaryMin:N0}";
                if (SalaryMax.HasValue)
                    return $"Up to RM {SalaryMax:N0}";
                return "Negotiable";
            }
        }
    }

    /// <summary>
    /// Helper class providing application status options and badge styling.
    /// </summary>
    public static class ApplicationStatusOptions
    {
        public static List<SelectListItem> GetStatuses() => new()
        {
            new SelectListItem("Pending", "Pending"),
            new SelectListItem("Reviewed", "Reviewed"),
            new SelectListItem("Shortlisted", "Shortlisted"),
            new SelectListItem("Interview", "Interview"),
            new SelectListItem("Offered", "Offered"),
            new SelectListItem("Hired", "Hired"),
            new SelectListItem("Rejected", "Rejected"),
            new SelectListItem("Withdrawn", "Withdrawn")
        };

        public static string GetStatusBadgeClass(string status) => status switch
        {
            "Pending" => "bg-warning text-dark",
            "Reviewed" => "bg-info",
            "Shortlisted" => "bg-primary",
            "Interview" => "bg-purple",
            "Offered" => "bg-success",
            "Hired" => "bg-success",
            "Rejected" => "bg-danger",
            "Withdrawn" => "bg-secondary",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// ViewModel for displaying application timeline events.
    /// </summary>
    public class ApplicationTimelineItemViewModel
    {
        public DateTime EventDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string EventType { get; set; } = "StatusUpdate"; // "Applied", "StatusUpdate", "Interview", "Offer"
        
        // Interview Details (if applicable)
        public DateTime? InterviewDate { get; set; }
        public string? InterviewTime { get; set; }
        public string? LocationOrLink { get; set; }
        public string? InterviewContactPerson { get; set; }
        public string? InterviewContactPhone { get; set; }
        public string? InterviewNotes { get; set; }
        public string? InterviewPdfPath { get; set; }
        
        // Offer Details (if applicable)
        public string? OfferJobTitle { get; set; }
        public decimal? SalaryOffered { get; set; }
        public DateTime? StartDate { get; set; }
        public string? EmploymentType { get; set; }
        public string? OfferContactPerson { get; set; }
        public string? OfferContactPhone { get; set; }
        public string? OfferNotes { get; set; }
        public string? OfferPdfPath { get; set; }
        
        // Helper properties
        public bool HasInterviewDetails => InterviewDate.HasValue;
        public bool HasOfferDetails => !string.IsNullOrEmpty(OfferJobTitle);
        public bool HasInterviewPdf => !string.IsNullOrEmpty(InterviewPdfPath);
        public bool HasOfferPdf => !string.IsNullOrEmpty(OfferPdfPath);
    }

    /// <summary>
    /// ViewModel for unified Application Chat page accessible by both Employer and JobSeeker.
    /// </summary>
    public class ApplicationChatViewModel
    {
        public int JobApplicationId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime ApplyDate { get; set; }
        
        /// <summary>
        /// The role of the current viewer (JobSeeker, Employer, or Admin).
        /// Used to determine message alignment and sender labels.
        /// </summary>
        public string ViewerRole { get; set; } = string.Empty;
        
        /// <summary>
        /// URL to navigate back to the appropriate application details page.
        /// </summary>
        public string? BackUrl { get; set; }
    }
}

