using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JobRequire.ViewModels
{
    // =============================================
    // JOB VACANCY VIEW MODELS
    // =============================================
    // Purpose: ViewModels for creating, editing, and listing job vacancies.
    // Includes: Create/Edit form, List view with filters.
    // 
    // Design Decision: Employers use these ViewModels to manage job postings.
    // Dropdown options are provided via static helper class.
    // =============================================

    /// <summary>
    /// ViewModel for creating and editing job vacancies.
    /// Used by Employers to post new jobs or update existing ones.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /JobVacancies/Create, /JobVacancies/Edit/{id}
    /// Includes dropdown options for category, job type, and status.
    /// </remarks>
    public class JobVacancyViewModel
    {
        public int JobVacancyId { get; set; }

        public int EmployerProfileId { get; set; }

        [Required(ErrorMessage = "Job title is required")]
        [StringLength(150, ErrorMessage = "Job title cannot exceed 150 characters")]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job type is required")]
        [StringLength(50, ErrorMessage = "Job type cannot exceed 50 characters")]
        [Display(Name = "Job Type")]
        public string JobType { get; set; } = string.Empty;

        [Display(Name = "Minimum Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
        [DataType(DataType.Currency)]
        public decimal? SalaryMin { get; set; }

        [Display(Name = "Maximum Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
        [DataType(DataType.Currency)]
        public decimal? SalaryMax { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
        [Display(Name = "Job Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "Requirements cannot exceed 4000 characters")]
        [Display(Name = "Requirements")]
        [DataType(DataType.MultilineText)]
        public string? Requirements { get; set; }

        [Display(Name = "Publish Date")]
        [DataType(DataType.Date)]
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Closing Date")]
        [DataType(DataType.Date)]
        public DateTime? ClosingDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Open";

        // For display
        public string? CompanyName { get; set; }
        public string? CompanyLogo { get; set; }
        public int ApplicationsCount { get; set; }

        // Dropdown options
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> JobTypeOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for paginated list of job vacancies with filters.
    /// Used by Employers to view and manage their job postings.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Employer/Jobs
    /// Supports filtering, sorting, and pagination.
    /// </remarks>
    public class JobVacancyListViewModel
    {
        public List<JobVacancyViewModel> Jobs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Filters
        public string? SearchTerm { get; set; }
        public string? CategoryFilter { get; set; }
        public string? JobTypeFilter { get; set; }
        public string? LocationFilter { get; set; }
        public string? StatusFilter { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }

        // Sorting
        public string SortBy { get; set; } = "PublishDate";
        public string SortOrder { get; set; } = "desc";

        // Filter options
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> JobTypes { get; set; } = new();
        public List<SelectListItem> Locations { get; set; } = new();
        public List<SelectListItem> Statuses { get; set; } = new();

        // Pagination helpers
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Helper class providing dropdown options for job vacancy forms.
    /// Contains predefined lists for categories, job types, locations, and statuses.
    /// </summary>
    public static class JobVacancyOptions
    {
        public static List<SelectListItem> GetCategories() => new()
        {
            new SelectListItem("Accounting & Finance", "Accounting & Finance"),
            new SelectListItem("Admin & HR", "Admin & HR"),
            new SelectListItem("Agriculture & Farming", "Agriculture & Farming"),
            new SelectListItem("Architecture & Interior Design", "Architecture & Interior Design"),
            new SelectListItem("Arts & Design", "Arts & Design"),
            new SelectListItem("Automotive", "Automotive"),
            new SelectListItem("Banking & Financial Services", "Banking & Financial Services"),
            new SelectListItem("Building & Construction", "Building & Construction"),
            new SelectListItem("Customer Service", "Customer Service"),
            new SelectListItem("E-Commerce", "E-Commerce"),
            new SelectListItem("Education & Training", "Education & Training"),
            new SelectListItem("Engineering", "Engineering"),
            new SelectListItem("Entertainment & Media", "Entertainment & Media"),
            new SelectListItem("F&B / Restaurant", "F&B / Restaurant"),
            new SelectListItem("Fashion & Apparel", "Fashion & Apparel"),
            new SelectListItem("Government & Public Sector", "Government & Public Sector"),
            new SelectListItem("Healthcare & Medical", "Healthcare & Medical"),
            new SelectListItem("Hospitality & Tourism", "Hospitality & Tourism"),
            new SelectListItem("Human Resources", "Human Resources"),
            new SelectListItem("Insurance", "Insurance"),
            new SelectListItem("IT & Software", "IT & Software"),
            new SelectListItem("Legal", "Legal"),
            new SelectListItem("Logistics & Supply Chain", "Logistics & Supply Chain"),
            new SelectListItem("Manufacturing", "Manufacturing"),
            new SelectListItem("Marketing & Communications", "Marketing & Communications"),
            new SelectListItem("Media & Advertising", "Media & Advertising"),
            new SelectListItem("NGO & Non-Profit", "NGO & Non-Profit"),
            new SelectListItem("Oil & Gas", "Oil & Gas"),
            new SelectListItem("Pharmaceutical", "Pharmaceutical"),
            new SelectListItem("Property & Real Estate", "Property & Real Estate"),
            new SelectListItem("Retail & Sales", "Retail & Sales"),
            new SelectListItem("Sciences", "Sciences"),
            new SelectListItem("Security & Safety", "Security & Safety"),
            new SelectListItem("Telecommunications", "Telecommunications"),
            new SelectListItem("Technology", "Technology"),
            new SelectListItem("Transportation", "Transportation"),
            new SelectListItem("Other", "Other")
        };

        public static List<SelectListItem> GetJobTypes() => new()
        {
            new SelectListItem("Full-Time", "Full-Time"),
            new SelectListItem("Part-Time", "Part-Time"),
            new SelectListItem("Contract", "Contract"),
            new SelectListItem("Internship", "Internship"),
            new SelectListItem("Freelance", "Freelance"),
            new SelectListItem("Temporary", "Temporary")
        };

        public static List<SelectListItem> GetLocations() => new()
        {
            // Major Cities
            new SelectListItem("Kuala Lumpur", "Kuala Lumpur"),
            new SelectListItem("KL", "KL"),
            new SelectListItem("Petaling Jaya", "Petaling Jaya"),
            new SelectListItem("Shah Alam", "Shah Alam"),
            new SelectListItem("Subang Jaya", "Subang Jaya"),
            new SelectListItem("Cyberjaya", "Cyberjaya"),
            new SelectListItem("Bangsar", "Bangsar"),
            new SelectListItem("KLCC", "KLCC"),
            new SelectListItem("Mont Kiara", "Mont Kiara"),
            new SelectListItem("Damansara", "Damansara"),
            new SelectListItem("Puchong", "Puchong"),
            new SelectListItem("Cheras", "Cheras"),
            new SelectListItem("Ampang", "Ampang"),
            new SelectListItem("Georgetown", "Georgetown"),
            new SelectListItem("Penang", "Penang"),
            new SelectListItem("Butterworth", "Butterworth"),
            new SelectListItem("Johor Bahru", "Johor Bahru"),
            new SelectListItem("Iskandar", "Iskandar"),
            new SelectListItem("Ipoh", "Ipoh"),
            new SelectListItem("Melaka", "Melaka"),
            new SelectListItem("Kota Kinabalu", "Kota Kinabalu"),
            new SelectListItem("Kuching", "Kuching"),
            // States
            new SelectListItem("Johor", "Johor"),
            new SelectListItem("Kedah", "Kedah"),
            new SelectListItem("Kelantan", "Kelantan"),
            new SelectListItem("Labuan", "Labuan"),
            new SelectListItem("Negeri Sembilan", "Negeri Sembilan"),
            new SelectListItem("Pahang", "Pahang"),
            new SelectListItem("Perak", "Perak"),
            new SelectListItem("Perlis", "Perlis"),
            new SelectListItem("Putrajaya", "Putrajaya"),
            new SelectListItem("Sabah", "Sabah"),
            new SelectListItem("Sarawak", "Sarawak"),
            new SelectListItem("Selangor", "Selangor"),
            new SelectListItem("Terengganu", "Terengganu"),
            // Special
            new SelectListItem("Remote", "Remote"),
            new SelectListItem("Work From Home", "Work From Home"),
            new SelectListItem("Hybrid", "Hybrid")
        };

        public static List<SelectListItem> GetStatuses() => new()
        {
            new SelectListItem("Open", "Open"),
            new SelectListItem("Closed", "Closed"),
            new SelectListItem("Draft", "Draft")
        };
    }
}

