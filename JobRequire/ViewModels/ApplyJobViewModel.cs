using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using JobRequire.Models;

namespace JobRequire.ViewModels
{
    // =============================================
    // JOB APPLICATION VIEW MODELS
    // =============================================
    // Purpose: ViewModels for the job application submission flow.
    // Includes: Apply form, Resume selection, Resume Fit Hint.
    // 
    // Design Decision: Supports both PDF upload and website resume.
    // Resume Fit Hint is informational only - does not block submission.
    // =============================================

    /// <summary>
    /// ViewModel for the job application form.
    /// Allows JobSeeker to select resume source and upload additional documents.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /JobApplications/Apply/{jobVacancyId}
    /// Includes job info for display and resume selection options.
    /// </remarks>
    public class ApplyJobViewModel
    {
        public int JobVacancyId { get; set; }

        // Job Info (for display)
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogo { get; set; }
        public string? Location { get; set; }
        public string? JobType { get; set; }

        // Resume Selection
        [Required(ErrorMessage = "Please select a resume option")]
        [Display(Name = "Resume Source")]
        public string? ResumeSource { get; set; } // Nullable - user must make explicit selection

        // PDF Resume Upload
        [Display(Name = "Upload Resume (PDF)")]
        public IFormFile? ResumeFile { get; set; }

        // Website Resume Summary (for display)
        public WebsiteResumeSummary? WebsiteResume { get; set; }

        // Resume Fit Hint (informational only)
        public ResumeFitHint? FitHint { get; set; }

        // Additional Documents
        [Display(Name = "Additional Documents")]
        public List<IFormFile>? AdditionalDocuments { get; set; }

        // Cover Letter (optional)
        [StringLength(4000)]
        [Display(Name = "Cover Letter")]
        public string? CoverLetter { get; set; }
    }

    /// <summary>
    /// Summary of the job seeker's website resume for display in Apply form
    /// </summary>
    public class WebsiteResumeSummary
    {
        public int JobSeekerProfileId { get; set; }
        public string? FullName { get; set; }
        public string? Summary { get; set; }

        // Counts
        public int EducationCount { get; set; }
        public int ExperienceCount { get; set; }
        public int SkillsCount { get; set; }

        // Latest entries for preview
        public string? LatestEducation { get; set; }
        public string? LatestExperience { get; set; }
        public List<string> TopSkills { get; set; } = new();

        // Completion
        public int CompletionPercentage { get; set; }

        // Validation
        public bool IsComplete => EducationCount > 0 || ExperienceCount > 0;
        public bool HasMinimumContent => EducationCount >= 1 || ExperienceCount >= 1;
        
        public string ValidationMessage
        {
            get
            {
                if (!HasMinimumContent)
                    return "Your resume must have at least 1 education or 1 work experience entry.";
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Resume-Job Fit Hint result for displaying match quality to job seekers
    /// </summary>
    public class ResumeFitHint
    {
        /// <summary>
        /// Whether the job seeker has a website resume (at least one section filled)
        /// </summary>
        public bool HasWebsiteResume { get; set; }

        /// <summary>
        /// Match level: "Good Match", "Partial Match", "Low Match", or null if no resume
        /// </summary>
        public string? MatchLevel { get; set; }

        /// <summary>
        /// Number of skills that matched the job requirements
        /// </summary>
        public int MatchedSkillsCount { get; set; }

        /// <summary>
        /// List of matched skill names (for display)
        /// </summary>
        public List<string> MatchedSkills { get; set; } = new();

        /// <summary>
        /// List of skills required by the job but missing from the resume
        /// Only populated for Partial Match or Low Match
        /// </summary>
        public List<string> MissingSkills { get; set; } = new();

        /// <summary>
        /// Whether there are missing skills to display (for Partial/Low Match only)
        /// </summary>
        public bool HasMissingSkills => MissingSkills.Any() && MatchLevel != "Good Match";

        /// <summary>
        /// CSS class for the match level badge
        /// </summary>
        public string MatchLevelBadgeClass => MatchLevel switch
        {
            "Good Match" => "bg-success",
            "Partial Match" => "bg-warning text-dark",
            "Low Match" => "bg-secondary",
            _ => "bg-light text-dark"
        };

        /// <summary>
        /// Icon class for the match level
        /// </summary>
        public string MatchLevelIcon => MatchLevel switch
        {
            "Good Match" => "bi-check-circle-fill text-success",
            "Partial Match" => "bi-dash-circle-fill text-warning",
            "Low Match" => "bi-x-circle-fill text-secondary",
            _ => "bi-question-circle text-muted"
        };
    }
}
