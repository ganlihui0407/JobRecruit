using System.ComponentModel.DataAnnotations;

namespace JobRequire.ViewModels
{
    // =============================================
    // COMPANY VIEW MODELS
    // =============================================
    // Purpose: ViewModels for public company directory and details.
    // Includes: Company list, Company details with job listings.
    // 
    // Design Decision: Public-facing views for browsing companies.
    // Different from EmployerProfileViewModel which is for editing.
    // =============================================

    /// <summary>
    /// ViewModel for displaying company card in the public directory.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Companies/Index
    /// Shows company summary with open job count.
    /// </remarks>
    public class CompanyViewModel
    {
        public int EmployerProfileId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string? Industry { get; set; }
        public string? CompanyDescription { get; set; }
        public string? CompanyAddress { get; set; }
        public int OpenJobsCount { get; set; }
    }

    /// <summary>
    /// ViewModel for the company details page.
    /// Shows company info and list of open job positions.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Companies/Details/{id}
    /// </remarks>
    public class CompanyDetailsViewModel
    {
        public int EmployerProfileId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string? Industry { get; set; }
        public string? CompanyDescription { get; set; }
        public string? CompanyAddress { get; set; }
        public int OpenJobsCount { get; set; }

        // Job listings
        public List<CompanyJobListingViewModel> OpenJobs { get; set; } = new();

        // For role-based display
        public bool IsOwnCompany { get; set; }
        public bool CanManage { get; set; }
    }

    /// <summary>
    /// ViewModel for a job listing shown on the company details page.
    /// Simplified version of JobListItemViewModel for company context.
    /// </summary>
    public class CompanyJobListingViewModel
    {
        public int JobVacancyId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        
        // Job Interest (Popularity) properties
        public int InterestedCount { get; set; }
        public bool IsPopular => InterestedCount >= 50;

        // Formatted salary display
        public string SalaryDisplay
        {
            get
            {
                if (!SalaryMin.HasValue && !SalaryMax.HasValue)
                    return "Negotiable";

                if (SalaryMin.HasValue && SalaryMax.HasValue)
                    return $"RM {SalaryMin:N0} - RM {SalaryMax:N0}";

                if (SalaryMin.HasValue)
                    return $"From RM {SalaryMin:N0}";

                return $"Up to RM {SalaryMax:N0}";
            }
        }

        // Days since posted
        public int DaysPosted => (DateTime.UtcNow - PublishDate).Days;

        public string PostedDisplay
        {
            get
            {
                if (DaysPosted == 0) return "Today";
                if (DaysPosted == 1) return "Yesterday";
                if (DaysPosted < 7) return $"{DaysPosted} days ago";
                if (DaysPosted < 30) return $"{DaysPosted / 7} weeks ago";
                return PublishDate.ToString("MMM dd, yyyy");
            }
        }
    }

    /// <summary>
    /// ViewModel for the paginated companies directory page.
    /// Supports search and industry filtering.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Companies/Index
    /// </remarks>
    public class CompanyListViewModel
    {
        public List<CompanyViewModel> Companies { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Filters
        public string? SearchTerm { get; set; }
        public string? IndustryFilter { get; set; }

        // Filter options
        public List<string> Industries { get; set; } = new();

        // Pagination helpers
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}



