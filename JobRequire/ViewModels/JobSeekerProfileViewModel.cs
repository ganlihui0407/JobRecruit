using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace JobRequire.ViewModels
{
    // =============================================
    // JOB SEEKER PROFILE VIEW MODELS
    // =============================================
    // Purpose: ViewModels for JobSeeker profile and dashboard.
    // Includes: Profile edit, Dashboard stats, Bookmarks, Public profile.
    // 
    // Design Decision: Dashboard shows application stats and recent activity.
    // Public profile supports sharing resume via unique URL.
    // =============================================

    /// <summary>
    /// ViewModel for editing JobSeeker profile information.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /JobSeekerProfiles/Edit
    /// </remarks>
    public class JobSeekerProfileViewModel
    {
        public int JobSeekerProfileId { get; set; }

        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(2000, ErrorMessage = "Summary cannot exceed 2000 characters")]
        [Display(Name = "Professional Summary")]
        [DataType(DataType.MultilineText)]
        public string? Summary { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhotoFile { get; set; }

        [Display(Name = "Current Photo")]
        public string? ProfilePhoto { get; set; }

        // For display
        public string? Username { get; set; }
        public string? Email { get; set; }

        // Resume sections
        public List<ResumeEducationViewModel> Education { get; set; } = new();
        public List<ResumeExperienceViewModel> Experience { get; set; } = new();
        public List<ResumeSkillViewModel> Skills { get; set; } = new();

        // Stats
        public int ApplicationsCount { get; set; }
        public int BookmarksCount { get; set; }
    }

    /// <summary>
    /// ViewModel for the JobSeeker dashboard main page.
    /// Contains profile summary, application stats, and recent activity.
    /// </summary>
    /// <remarks>
    /// Usage: GET /JobSeekerProfiles/Dashboard
    /// </remarks>
    public class JobSeekerDashboardViewModel
    {
        public JobSeekerProfileViewModel Profile { get; set; } = new();
        public List<JobApplicationViewModel> RecentApplications { get; set; } = new();
        public List<BookmarkedJobViewModel> RecentBookmarks { get; set; } = new();
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedApplications { get; set; }
        public int RejectedApplications { get; set; }
        
        /// <summary>
        /// Profile completion percentage (0-100), calculated by ResumeCompletionService.
        /// This value is synchronized with the Resume Builder completion.
        /// </summary>
        public int ProfileCompletion { get; set; }

        /// <summary>
        /// Public profile URL for sharing.
        /// Format: /Profile/Public/{identifier}
        /// </summary>
        public string? PublicProfileUrl { get; set; }

        /// <summary>
        /// Public identifier for the profile (GUID-based for security).
        /// </summary>
        public string? PublicIdentifier { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying a bookmarked job in the JobSeeker's saved jobs list.
    /// </summary>
    public class BookmarkedJobViewModel
    {
        public int BookmarkedJobId { get; set; }
        public int JobSeekerProfileId { get; set; }
        public int JobVacancyId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Job info
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? Location { get; set; }
        public string? JobType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// ViewModel for public profile sharing page.
    /// Contains only public-safe information (no passwords, internal IDs, or verification data).
    /// </summary>
    public class PublicProfileViewModel
    {
        /// <summary>
        /// Public identifier for the profile (GUID-based, not internal ID)
        /// </summary>
        public string PublicIdentifier { get; set; } = string.Empty;

        // Basic Profile Info
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Summary { get; set; }
        public string? ProfilePhoto { get; set; }

        // Resume Data (read-only)
        public ResumeSnapshotViewModel? Resume { get; set; }

        // Profile Stats (public-safe)
        public int TotalExperience { get; set; }
        public int TotalEducation { get; set; }
        public int TotalSkills { get; set; }

        // Metadata
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}

