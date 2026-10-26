using System.ComponentModel.DataAnnotations;

namespace JobRequire.ViewModels
{
    // =============================================
    // JOB SEARCH VIEW MODELS
    // =============================================
    // Purpose: ViewModels for public job search and browsing.
    // Includes: Search filters, Search results, Job list items.
    // 
    // Design Decision: Used by both anonymous and authenticated users.
    // Bookmark and Interest status is populated only for logged-in JobSeekers.
    // =============================================

    /// <summary>
    /// ViewModel for job search filter form.
    /// Contains all filter criteria for job search.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Jobs/Search, /Jobs/Index
    /// Filters are applied via query string parameters.
    /// </remarks>
    public class JobSearchViewModel
    {
        [Display(Name = "Keyword")]
        public string? Keyword { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Job Type")]
        public string? JobType { get; set; }

        [Display(Name = "Min Salary")]
        [Range(0, double.MaxValue)]
        public decimal? MinSalary { get; set; }

        [Display(Name = "Max Salary")]
        [Range(0, double.MaxValue)]
        public decimal? MaxSalary { get; set; }

        [Display(Name = "Sort By")]
        public string SortBy { get; set; } = "newest";

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// ViewModel for paginated job search results.
    /// Contains job list, pagination info, and filter options for dropdowns.
    /// </summary>
    /// <remarks>
    /// Usage: Returned by Jobs controller to display search results page.
    /// </remarks>
    public class JobSearchResultViewModel
    {
        public List<JobListItemViewModel> Jobs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public JobSearchViewModel Filters { get; set; } = new();

        // Filter options for dropdowns
        public List<string> Categories { get; set; } = new();
        public List<string> Locations { get; set; } = new();
        public List<string> JobTypes { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for a single job item in search results or listings.
    /// Contains job details, company info, and user-specific state (bookmark, applied).
    /// </summary>
    /// <remarks>
    /// Usage: Used in job cards throughout the application.
    /// Computed properties provide formatted display values.
    /// </remarks>
    public class JobListItemViewModel
    {
        public int JobVacancyId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public int EmployerProfileId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Description { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsBookmarked { get; set; }
        public bool HasApplied { get; set; }
        
        // Job Interest (Popularity) properties
        public int InterestedCount { get; set; }
        public bool IsInterested { get; set; }
        
        /// <summary>
        /// A job is considered "Popular" if it has 50 or more interested job seekers.
        /// </summary>
        public bool IsPopular => InterestedCount >= 50;

        // Computed properties
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

        public string TimeAgo
        {
            get
            {
                var span = DateTime.UtcNow - PublishDate;
                if (span.TotalDays >= 30)
                    return $"{(int)(span.TotalDays / 30)} month(s) ago";
                if (span.TotalDays >= 7)
                    return $"{(int)(span.TotalDays / 7)} week(s) ago";
                if (span.TotalDays >= 1)
                    return $"{(int)span.TotalDays} day(s) ago";
                if (span.TotalHours >= 1)
                    return $"{(int)span.TotalHours} hour(s) ago";
                return "Just now";
            }
        }

        public bool IsClosingSoon => ClosingDate.HasValue && ClosingDate.Value <= DateTime.UtcNow.AddDays(7);
        public bool IsClosed => ClosingDate.HasValue && ClosingDate.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// API response DTO for job search endpoints.
    /// Used by AJAX calls for dynamic job loading.
    /// </summary>
    public class JobSearchApiResponse
    {
        public bool Success { get; set; }
        public List<JobListItemViewModel> Jobs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// API response DTO for bookmark toggle operations.
    /// </summary>
    public class BookmarkApiResponse
    {
        public bool Success { get; set; }
        public bool IsBookmarked { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

