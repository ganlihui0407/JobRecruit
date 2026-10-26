using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace JobRequire.ViewModels
{
    // =============================================
    // EMPLOYER PROFILE VIEW MODELS
    // =============================================
    // Purpose: ViewModels for Employer profile management.
    // Includes: Profile edit, Profile list (admin view).
    // =============================================

    /// <summary>
    /// ViewModel for editing Employer (company) profile information.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /EmployerProfiles/Edit
    /// Includes company logo upload support.
    /// </remarks>
    public class EmployerProfileViewModel
    {
        public int EmployerProfileId { get; set; }

        public int UserId { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [StringLength(150, ErrorMessage = "Company name cannot exceed 150 characters")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
        [Display(Name = "Company Address")]
        public string? CompanyAddress { get; set; }

        [StringLength(100, ErrorMessage = "Industry cannot exceed 100 characters")]
        [Display(Name = "Industry")]
        public string? Industry { get; set; }

        [Display(Name = "Company Logo")]
        public IFormFile? CompanyLogoFile { get; set; }

        [Display(Name = "Current Logo")]
        public string? CompanyLogo { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Company Description")]
        [DataType(DataType.MultilineText)]
        public string? CompanyDescription { get; set; }

        // For display
        public string? Username { get; set; }
        public string? Email { get; set; }
        public int JobVacanciesCount { get; set; }
    }

    /// <summary>
    /// ViewModel for paginated list of employer profiles.
    /// Used by admin to view all employers.
    /// </summary>
    public class EmployerProfileListViewModel
    {
        public List<EmployerProfileViewModel> Profiles { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? IndustryFilter { get; set; }
        public List<string> Industries { get; set; } = new();

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

