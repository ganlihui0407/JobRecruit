using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.ViewModels;
using System.Security.Claims;
using System.Text;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for Employer dashboard and management
    /// </summary>
    [Authorize(Roles = "Employer,Admin")]
    public class EmployerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployerController> _logger;

        public EmployerController(
            ApplicationDbContext context,
            ILogger<EmployerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: Employer/Dashboard - Main employer dashboard
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var employerProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employerProfile == null)
            {
                TempData["ErrorMessage"] = "Please complete your employer profile first.";
                return RedirectToAction("Create", "EmployerProfiles");
            }

            var viewModel = await BuildDashboardViewModelAsync(employerProfile.EmployerProfileId);

            return View(viewModel);
        }

        /// <summary>
        /// GET: Employer/ExportApplicants/{jobVacancyId} - Export applicants as CSV
        /// </summary>
        public async Task<IActionResult> ExportApplicants(int jobVacancyId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var employerProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employerProfile == null)
                return Forbid();

            // Verify the job belongs to this employer
            var jobVacancy = await _context.JobVacancies
                .Where(j => j.JobVacancyId == jobVacancyId && j.EmployerProfileId == employerProfile.EmployerProfileId)
                .Select(j => new { j.JobTitle })
                .FirstOrDefaultAsync();

            if (jobVacancy == null)
            {
                TempData["ErrorMessage"] = "Job not found or you don't have permission to access it.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Get applicants with optimized projection (no Include needed with Select)
            var applicants = await _context.JobApplications
                .Where(ja => ja.JobVacancyId == jobVacancyId)
                .Select(ja => new ApplicantExportViewModel
                {
                    JobApplicationId = ja.JobApplicationId,
                    JobTitle = ja.JobVacancy.JobTitle,
                    ApplicantName = ja.JobSeekerProfile.FullName,
                    ApplicantEmail = ja.JobSeekerProfile.User.Email,
                    PhoneNumber = ja.JobSeekerProfile.PhoneNumber,
                    ApplyDate = ja.ApplyDate,
                    Status = ja.Status
                })
                .OrderBy(a => a.ApplyDate)
                .ToListAsync();

            // Generate CSV
            var csv = GenerateCsv(applicants, jobVacancy.JobTitle);

            var fileName = $"Applicants_{jobVacancy.JobTitle.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        /// <summary>
        /// Build dashboard view model with optimized queries
        /// </summary>
        private async Task<EmployerDashboardViewModel> BuildDashboardViewModelAsync(int employerProfileId)
        {
            var viewModel = new EmployerDashboardViewModel();

            // Get job vacancies with applicant counts (optimized projection)
            // Exclude "Withdrawn" applications from all counts
            var jobVacanciesQuery = _context.JobVacancies
                .Where(j => j.EmployerProfileId == employerProfileId)
                .Select(j => new
                {
                    j.JobVacancyId,
                    j.JobTitle,
                    j.Category,
                    j.JobType,
                    j.Location,
                    j.SalaryMin,
                    j.SalaryMax,
                    j.Status,
                    j.PublishDate,
                    j.ClosingDate,
                    TotalApplications = j.JobApplications.Count(ja => ja.Status != "Withdrawn"),
                    PendingApplications = j.JobApplications.Count(ja => ja.Status == "Pending"),
                    ShortlistedApplications = j.JobApplications.Count(ja => ja.Status == "Shortlisted"),
                    InterviewApplications = j.JobApplications.Count(ja => ja.Status == "Interview"),
                    HiredApplications = j.JobApplications.Count(ja => ja.Status == "Hired"),
                    RejectedApplications = j.JobApplications.Count(ja => ja.Status == "Rejected")
                })
                .OrderByDescending(j => j.PublishDate);

            var jobVacancies = await jobVacanciesQuery.ToListAsync();

            // Map to view model
            viewModel.JobVacancies = jobVacancies.Select(j => new JobVacancySummaryViewModel
            {
                JobVacancyId = j.JobVacancyId,
                JobTitle = j.JobTitle,
                Category = j.Category,
                JobType = j.JobType,
                Location = j.Location,
                SalaryMin = j.SalaryMin,
                SalaryMax = j.SalaryMax,
                Status = j.Status,
                PublishDate = j.PublishDate,
                ClosingDate = j.ClosingDate,
                TotalApplications = j.TotalApplications,
                PendingApplications = j.PendingApplications,
                ShortlistedApplications = j.ShortlistedApplications,
                InterviewApplications = j.InterviewApplications,
                HiredApplications = j.HiredApplications,
                RejectedApplications = j.RejectedApplications
            }).ToList();

            // Calculate summary statistics
            viewModel.TotalJobs = jobVacancies.Count;
            viewModel.OpenJobs = jobVacancies.Count(j => j.Status == "Open");
            viewModel.ClosedJobs = jobVacancies.Count(j => j.Status == "Closed");
            viewModel.TotalApplications = jobVacancies.Sum(j => j.TotalApplications);
            viewModel.PendingApplications = jobVacancies.Sum(j => j.PendingApplications);
            viewModel.ShortlistedApplications = jobVacancies.Sum(j => j.ShortlistedApplications);
            viewModel.HiredCount = jobVacancies.Sum(j => j.HiredApplications);

            // Chart data: Applicants per job (top 10)
            viewModel.ApplicantPerJobChart = jobVacancies
                .Where(j => j.TotalApplications > 0)
                .OrderByDescending(j => j.TotalApplications)
                .Take(10)
                .Select(j => new ApplicantPerJobChartData
                {
                    JobTitle = j.JobTitle.Length > 30 ? j.JobTitle.Substring(0, 30) + "..." : j.JobTitle,
                    ApplicantCount = j.TotalApplications
                })
                .ToList();

            // Chart data: Jobs posted by month (last 12 months)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var jobsByMonth = jobVacancies
                .Where(j => j.PublishDate >= twelveMonthsAgo)
                .GroupBy(j => new { Year = j.PublishDate.Year, Month = j.PublishDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            viewModel.JobsPostedByMonthChart = jobsByMonth
                .Select(g => new JobsPostedByMonthChartData
                {
                    Month = new DateTime(g.Year, g.Month, 1).ToString("MMM yyyy"),
                    JobCount = g.Count
                })
                .ToList();

            // ========== DECISION DATA ==========

            // 1. Applications by Status
            var jobVacancyIds = jobVacancies.Select(j => j.JobVacancyId).ToList();
            var applicationStatusCounts = await _context.JobApplications
                .Where(ja => jobVacancyIds.Contains(ja.JobVacancyId) && ja.Status != "Withdrawn")
                .GroupBy(ja => ja.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            viewModel.ApplicationsByStatus = applicationStatusCounts
                .OrderByDescending(s => s.Count)
                .Select(s => new ApplicationStatusCountViewModel
                {
                    Status = s.Status,
                    Count = s.Count,
                    BadgeClass = GetStatusBadgeClass(s.Status),
                    Icon = GetStatusIcon(s.Status)
                })
                .ToList();

            // 2. Most Interested Jobs (Top 3)
            viewModel.MostInterestedJobs = await _context.JobVacancies
                .Where(j => j.EmployerProfileId == employerProfileId)
                .Select(j => new MostInterestedJobViewModel
                {
                    JobVacancyId = j.JobVacancyId,
                    JobTitle = j.JobTitle,
                    InterestedCount = j.JobInterests.Count
                })
                .OrderByDescending(j => j.InterestedCount)
                .Take(3)
                .ToListAsync();

            // 3. Jobs Posted in Last 30 Days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            viewModel.JobsPostedLast30Days = jobVacancies.Count(j => j.PublishDate >= thirtyDaysAgo);

            return viewModel;
        }

        /// <summary>
        /// Get Bootstrap badge class for application status
        /// </summary>
        private static string GetStatusBadgeClass(string status) => status switch
        {
            "Pending" => "bg-warning text-dark",
            "Reviewed" => "bg-info",
            "Shortlisted" => "bg-primary",
            "Interview" => "bg-purple",
            "Offered" => "bg-success",
            "Hired" => "bg-success",
            "Rejected" => "bg-danger",
            _ => "bg-secondary"
        };

        /// <summary>
        /// Get Bootstrap icon for application status
        /// </summary>
        private static string GetStatusIcon(string status) => status switch
        {
            "Pending" => "bi-hourglass-split",
            "Reviewed" => "bi-eye",
            "Shortlisted" => "bi-check-circle",
            "Interview" => "bi-calendar-event",
            "Offered" => "bi-gift",
            "Hired" => "bi-trophy",
            "Rejected" => "bi-x-circle",
            _ => "bi-circle"
        };

        /// <summary>
        /// Generate CSV content from applicant list
        /// </summary>
        private string GenerateCsv(List<ApplicantExportViewModel> applicants, string jobTitle)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine($"Job Title: {jobTitle}");
            csv.AppendLine($"Export Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine();
            
            // Column headers
            csv.AppendLine("Application ID,Applicant Name,Email,Phone Number,Apply Date,Status");
            
            // Data rows
            foreach (var applicant in applicants)
            {
                csv.AppendLine($"{applicant.JobApplicationId}," +
                             $"\"{EscapeCsvField(applicant.ApplicantName)}\"," +
                             $"\"{EscapeCsvField(applicant.ApplicantEmail)}\"," +
                             $"\"{EscapeCsvField(applicant.PhoneNumber ?? "")}\"," +
                             $"{applicant.ApplyDate:yyyy-MM-dd HH:mm:ss}," +
                             $"\"{EscapeCsvField(applicant.Status)}\"");
            }
            
            return csv.ToString();
        }

        /// <summary>
        /// Escape CSV field (handle quotes and commas)
        /// </summary>
        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;
            
            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return field.Replace("\"", "\"\"");
            }
            
            return field;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}

