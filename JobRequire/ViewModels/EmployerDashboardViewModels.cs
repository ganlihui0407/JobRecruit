namespace JobRequire.ViewModels
{
    // =============================================
    // EMPLOYER DASHBOARD VIEW MODELS
    // =============================================
    // Purpose: ViewModels for the Employer dashboard and analytics.
    // Includes: Dashboard stats, Job summaries, Chart data, Export data.
    // 
    // Design Decision: Dashboard provides at-a-glance metrics.
    // Chart data ViewModels are used for JavaScript chart rendering.
    // =============================================

    /// <summary>
    /// ViewModel for the Employer dashboard main page.
    /// Contains summary statistics and recent activity.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Employer/Dashboard
    /// Displays job stats, application counts, and charts.
    /// </remarks>
    public class EmployerDashboardViewModel
    {
        public int TotalJobs { get; set; }
        public int OpenJobs { get; set; }
        public int ClosedJobs { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedApplications { get; set; }
        public int HiredCount { get; set; }
        
        public List<JobVacancySummaryViewModel> JobVacancies { get; set; } = new();
        public List<ApplicantPerJobChartData> ApplicantPerJobChart { get; set; } = new();
        public List<JobsPostedByMonthChartData> JobsPostedByMonthChart { get; set; } = new();
        
        // Decision Data - Applications by Status
        public List<ApplicationStatusCountViewModel> ApplicationsByStatus { get; set; } = new();
        
        // Decision Data - Most Interested Jobs (Top 3)
        public List<MostInterestedJobViewModel> MostInterestedJobs { get; set; } = new();
        
        // Decision Data - Jobs Posted in Last 30 Days
        public int JobsPostedLast30Days { get; set; }
    }

    /// <summary>
    /// Summary of a job vacancy for dashboard listing
    /// </summary>
    public class JobVacancySummaryViewModel
    {
        public int JobVacancyId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedApplications { get; set; }
        public int InterviewApplications { get; set; }
        public int HiredApplications { get; set; }
        public int RejectedApplications { get; set; }
    }

    /// <summary>
    /// Chart data for applicants per job
    /// </summary>
    public class ApplicantPerJobChartData
    {
        public string JobTitle { get; set; } = string.Empty;
        public int ApplicantCount { get; set; }
    }

    /// <summary>
    /// Chart data for jobs posted by month
    /// </summary>
    public class JobsPostedByMonthChartData
    {
        public string Month { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }

    /// <summary>
    /// View model for CSV export of applicants
    /// </summary>
    public class ApplicantExportViewModel
    {
        public int JobApplicationId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime ApplyDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for application count by status
    /// </summary>
    public class ApplicationStatusCountViewModel
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public string BadgeClass { get; set; } = "bg-secondary";
        public string Icon { get; set; } = "bi-circle";
    }

    /// <summary>
    /// View model for most interested jobs
    /// </summary>
    public class MostInterestedJobViewModel
    {
        public int JobVacancyId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public int InterestedCount { get; set; }
        public bool IsPopular => InterestedCount >= 50;
    }
}

