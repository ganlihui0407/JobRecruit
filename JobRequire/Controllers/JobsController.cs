using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.Services;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IJobInterestService _interestService;

        // Default page size, can be overridden in appsettings.json
        private int DefaultPageSize => _configuration.GetValue<int>("JobSearch:DefaultPageSize", 10);
        private int MaxPageSize => _configuration.GetValue<int>("JobSearch:MaxPageSize", 50);

        public JobsController(
            ApplicationDbContext context,
            ILogger<JobsController> logger,
            IConfiguration configuration,
            IJobInterestService interestService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _interestService = interestService;
        }

        #region Main Views

        /// <summary>
        /// GET: Jobs - Main job search page
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Index(JobSearchViewModel filters)
        {
            filters.PageSize = Math.Clamp(filters.PageSize, 1, MaxPageSize);
            if (filters.PageSize == 0) filters.PageSize = DefaultPageSize;

            var result = await SearchJobsAsync(filters);
            
            // Load filter options
            result.Categories = await GetDistinctCategoriesAsync();
            result.Locations = await GetDistinctLocationsAsync();
            result.JobTypes = await GetDistinctJobTypesAsync();

            return View(result);
        }

        /// <summary>
        /// GET: Jobs/Search - Public search endpoint (redirects to Index with params)
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Search(string? keyword, string? location, string? category)
        {
            var filters = new JobSearchViewModel
            {
                Keyword = keyword,
                Location = location,
                Category = category,
                PageNumber = 1,
                PageSize = DefaultPageSize
            };

            var result = await SearchJobsAsync(filters);
            
            // Load filter options
            result.Categories = await GetDistinctCategoriesAsync();
            result.Locations = await GetDistinctLocationsAsync();
            result.JobTypes = await GetDistinctJobTypesAsync();

            return View("Index", result);
        }

        /// <summary>
        /// GET: Jobs/Details/{id} - Job details page
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(j => j.JobVacancyId == id && j.Status == "Open");

            if (job == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            
            // Get interest data for the main job
            var interestCounts = await _interestService.GetInterestedCountsAsync(new[] { job.JobVacancyId });
            Dictionary<int, bool> userInterests = new();
            if (userId.HasValue)
            {
                var profile = await _context.JobSeekerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    userInterests = await _interestService.GetUserInterestsAsync(new[] { job.JobVacancyId }, profile.JobSeekerProfileId);
                }
            }
            
            var viewModel = MapToListItem(job, userId, interestCounts, userInterests);

            // Get related jobs
            var relatedJobs = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .Where(j => j.JobVacancyId != id &&
                           j.Status == "Open" &&
                           (j.Category == job.Category || j.Location == job.Location))
                .OrderByDescending(j => j.PublishDate)
                .Take(5)
                .ToListAsync();

            // Get interest data for related jobs
            var relatedJobIds = relatedJobs.Select(j => j.JobVacancyId).ToList();
            var relatedInterestCounts = await _interestService.GetInterestedCountsAsync(relatedJobIds);
            Dictionary<int, bool> relatedUserInterests = new();
            if (userId.HasValue)
            {
                var profile = await _context.JobSeekerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    relatedUserInterests = await _interestService.GetUserInterestsAsync(relatedJobIds, profile.JobSeekerProfileId);
                }
            }

            ViewBag.RelatedJobs = relatedJobs.Select(j => MapToListItem(j, userId, relatedInterestCounts, relatedUserInterests)).ToList();
            ViewBag.FullDescription = job.Description;
            ViewBag.Requirements = job.Requirements;

            return View(viewModel);
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// GET: /api/jobs/search - AJAX search endpoint
        /// </summary>
        [HttpGet]
        [Route("api/jobs/search")]
        public async Task<IActionResult> SearchApi([FromQuery] JobSearchViewModel filters)
        {
            try
            {
                filters.PageSize = Math.Clamp(filters.PageSize, 1, MaxPageSize);
                if (filters.PageSize == 0) filters.PageSize = DefaultPageSize;

                var result = await SearchJobsAsync(filters);

                return Json(new JobSearchApiResponse
                {
                    Success = true,
                    Jobs = result.Jobs,
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalPages = result.TotalPages,
                    HasPreviousPage = result.HasPreviousPage,
                    HasNextPage = result.HasNextPage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job search API");
                return Json(new JobSearchApiResponse
                {
                    Success = false,
                    Message = "An error occurred while searching for jobs."
                });
            }
        }

        /// <summary>
        /// POST: /api/jobs/bookmark - Toggle bookmark for a job
        /// </summary>
        [HttpPost]
        [Route("api/jobs/bookmark")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> ToggleBookmark([FromBody] BookmarkRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new BookmarkApiResponse
                    {
                        Success = false,
                        Message = "Please log in to bookmark jobs."
                    });
                }

                var profile = await _context.JobSeekerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    return Json(new BookmarkApiResponse
                    {
                        Success = false,
                        Message = "Job seeker profile not found."
                    });
                }

                // Check if job exists
                var jobExists = await _context.JobVacancies
                    .AnyAsync(j => j.JobVacancyId == request.JobVacancyId);

                if (!jobExists)
                {
                    return Json(new BookmarkApiResponse
                    {
                        Success = false,
                        Message = "Job not found."
                    });
                }

                var existingBookmark = await _context.BookmarkedJobs
                    .FirstOrDefaultAsync(b => b.JobSeekerProfileId == profile.JobSeekerProfileId &&
                                              b.JobVacancyId == request.JobVacancyId);

                if (existingBookmark != null)
                {
                    // Remove bookmark
                    _context.BookmarkedJobs.Remove(existingBookmark);
                    await _context.SaveChangesAsync();

                    return Json(new BookmarkApiResponse
                    {
                        Success = true,
                        IsBookmarked = false,
                        Message = "Job removed from bookmarks."
                    });
                }
                else
                {
                    // Add bookmark
                    var bookmark = new BookmarkedJob
                    {
                        JobSeekerProfileId = profile.JobSeekerProfileId,
                        JobVacancyId = request.JobVacancyId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.BookmarkedJobs.Add(bookmark);
                    await _context.SaveChangesAsync();

                    return Json(new BookmarkApiResponse
                    {
                        Success = true,
                        IsBookmarked = true,
                        Message = "Job bookmarked successfully."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling bookmark");
                return Json(new BookmarkApiResponse
                {
                    Success = false,
                    Message = "An error occurred while bookmarking the job."
                });
            }
        }

        /// <summary>
        /// GET: /api/jobs/filters - Get filter options
        /// </summary>
        [HttpGet]
        [Route("api/jobs/filters")]
        public async Task<IActionResult> GetFilters()
        {
            return Json(new
            {
                success = true,
                categories = await GetDistinctCategoriesAsync(),
                locations = await GetDistinctLocationsAsync(),
                jobTypes = await GetDistinctJobTypesAsync()
            });
        }

        #endregion

        #region Partial Views

        /// <summary>
        /// GET: Jobs/JobListPartial - Returns partial view for AJAX updates
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> JobListPartial(JobSearchViewModel filters)
        {
            filters.PageSize = Math.Clamp(filters.PageSize, 1, MaxPageSize);
            if (filters.PageSize == 0) filters.PageSize = DefaultPageSize;

            var result = await SearchJobsAsync(filters);
            return PartialView("_JobListPartial", result);
        }

        #endregion

        #region Private Methods

        private async Task<JobSearchResultViewModel> SearchJobsAsync(JobSearchViewModel filters)
        {
            var query = _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .Where(j => j.Status == "Open") // Only show open jobs
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filters.Keyword))
            {
                var keyword = filters.Keyword.ToLower();
                query = query.Where(j =>
                    j.JobTitle.ToLower().Contains(keyword) ||
                    (j.Description != null && j.Description.ToLower().Contains(keyword)) ||
                    (j.EmployerProfile != null && j.EmployerProfile.CompanyName.ToLower().Contains(keyword)) ||
                    (j.Requirements != null && j.Requirements.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(filters.Location))
            {
                query = query.Where(j => j.Location.ToLower().Contains(filters.Location.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filters.Category))
            {
                query = query.Where(j => j.Category == filters.Category);
            }

            if (!string.IsNullOrWhiteSpace(filters.JobType))
            {
                query = query.Where(j => j.JobType == filters.JobType);
            }

            if (filters.MinSalary.HasValue)
            {
                query = query.Where(j => j.SalaryMax >= filters.MinSalary || j.SalaryMin >= filters.MinSalary);
            }

            if (filters.MaxSalary.HasValue)
            {
                query = query.Where(j => j.SalaryMin <= filters.MaxSalary || j.SalaryMax <= filters.MaxSalary);
            }

            // Get total count before paging
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filters.SortBy?.ToLower() switch
            {
                "newest" => query.OrderByDescending(j => j.PublishDate),
                "oldest" => query.OrderBy(j => j.PublishDate),
                "salary_high" => query.OrderByDescending(j => j.SalaryMax ?? j.SalaryMin ?? 0),
                "salary_low" => query.OrderBy(j => j.SalaryMin ?? j.SalaryMax ?? decimal.MaxValue),
                "title" => query.OrderBy(j => j.JobTitle),
                "company" => query.OrderBy(j => j.EmployerProfile.CompanyName),
                "closing" => query.OrderBy(j => j.ClosingDate ?? DateTime.MaxValue),
                // Default: sort by popularity (InterestedCount DESC), then by PublishDate DESC
                _ => query.OrderByDescending(j => j.JobInterests.Count).ThenByDescending(j => j.PublishDate)
            };

            // Apply paging
            var jobs = await query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToListAsync();

            var userId = GetCurrentUserId();
            var jobIds = jobs.Select(j => j.JobVacancyId).ToList();

            // Get interest counts for all jobs in one query
            var interestCounts = await _interestService.GetInterestedCountsAsync(jobIds);

            // Get user's interests if logged in as JobSeeker
            Dictionary<int, bool> userInterests = new();
            if (userId.HasValue)
            {
                var profile = await _context.JobSeekerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    userInterests = await _interestService.GetUserInterestsAsync(jobIds, profile.JobSeekerProfileId);
                }
            }

            return new JobSearchResultViewModel
            {
                Jobs = jobs.Select(j => MapToListItem(j, userId, interestCounts, userInterests)).ToList(),
                TotalCount = totalCount,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize,
                Filters = filters
            };
        }

        private JobListItemViewModel MapToListItem(
            JobVacancy job, 
            int? userId, 
            Dictionary<int, int>? interestCounts = null, 
            Dictionary<int, bool>? userInterests = null)
        {
            var isBookmarked = false;
            var hasApplied = false;

            if (userId.HasValue)
            {
                var profile = _context.JobSeekerProfiles
                    .FirstOrDefault(p => p.UserId == userId);

                if (profile != null)
                {
                    // Check if THIS user has bookmarked THIS job
                    isBookmarked = _context.BookmarkedJobs
                        .Any(b => b.JobSeekerProfileId == profile.JobSeekerProfileId &&
                                  b.JobVacancyId == job.JobVacancyId);

                    // Check if THIS user has applied to THIS job with a non-withdrawn status
                    // This is per-user check: other jobseekers can still apply to the same job
                    // Withdrawn applications do NOT block re-application
                    hasApplied = _context.JobApplications
                        .Any(a => a.JobSeekerProfileId == profile.JobSeekerProfileId &&
                                  a.JobVacancyId == job.JobVacancyId &&
                                  a.Status != "Withdrawn");
                }
            }

            // Get interest data from pre-loaded dictionaries or default to 0/false
            var interestedCount = interestCounts?.GetValueOrDefault(job.JobVacancyId, 0) ?? 0;
            var isInterested = userInterests?.GetValueOrDefault(job.JobVacancyId, false) ?? false;

            return new JobListItemViewModel
            {
                JobVacancyId = job.JobVacancyId,
                JobTitle = job.JobTitle,
                CompanyName = job.EmployerProfile?.CompanyName ?? "Unknown Company",
                CompanyLogo = job.EmployerProfile?.CompanyLogo,
                EmployerProfileId = job.EmployerProfileId,
                Location = job.Location,
                Category = job.Category,
                JobType = job.JobType,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Description = TruncateDescription(job.Description, 200),
                PublishDate = job.PublishDate,
                ClosingDate = job.ClosingDate,
                Status = job.Status,
                IsBookmarked = isBookmarked,
                HasApplied = hasApplied,
                InterestedCount = interestedCount,
                IsInterested = isInterested
            };
        }

        private static string TruncateDescription(string? description, int maxLength)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            // Remove HTML tags if any
            var plainText = System.Text.RegularExpressions.Regex.Replace(description, "<[^>]*>", "");

            if (plainText.Length <= maxLength)
                return plainText;

            return plainText.Substring(0, maxLength).TrimEnd() + "...";
        }

        private async Task<List<string>> GetDistinctCategoriesAsync()
        {
            // Predefined categories for Malaysian job market
            var predefinedCategories = new List<string>
            {
                "Accounting & Finance",
                "Admin & HR",
                "Agriculture & Farming",
                "Architecture & Interior Design",
                "Arts & Design",
                "Automotive",
                "Banking & Financial Services",
                "Building & Construction",
                "Customer Service",
                "E-Commerce",
                "Education & Training",
                "Engineering",
                "Entertainment & Media",
                "F&B / Restaurant",
                "Fashion & Apparel",
                "Government & Public Sector",
                "Healthcare & Medical",
                "Hospitality & Tourism",
                "Human Resources",
                "Insurance",
                "IT & Software",
                "Legal",
                "Logistics & Supply Chain",
                "Manufacturing",
                "Marketing & Communications",
                "Media & Advertising",
                "NGO & Non-Profit",
                "Oil & Gas",
                "Pharmaceutical",
                "Property & Real Estate",
                "Retail & Sales",
                "Sciences",
                "Security & Safety",
                "Telecommunications",
                "Technology",
                "Transportation",
                "Other"
            };

            // Get categories from database and merge with predefined
            var dbCategories = await _context.JobVacancies
                .Where(j => j.Status == "Open" && !string.IsNullOrEmpty(j.Category))
                .Select(j => j.Category)
                .Distinct()
                .ToListAsync();

            return predefinedCategories
                .Union(dbCategories)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        private async Task<List<string>> GetDistinctLocationsAsync()
        {
            // Predefined locations for Malaysian states and major cities
            var predefinedLocations = new List<string>
            {
                // Major Cities
                "Kuala Lumpur",
                "KL",
                "Petaling Jaya",
                "Shah Alam",
                "Subang Jaya",
                "Cyberjaya",
                "Bangsar",
                "KLCC",
                "Mont Kiara",
                "Damansara",
                "Puchong",
                "Cheras",
                "Ampang",
                "Georgetown",
                "Penang",
                "Butterworth",
                "Johor Bahru",
                "Iskandar",
                "Ipoh",
                "Melaka",
                "Kota Kinabalu",
                "Kuching",
                // States
                "Johor",
                "Kedah",
                "Kelantan",
                "Labuan",
                "Negeri Sembilan",
                "Pahang",
                "Perak",
                "Perlis",
                "Putrajaya",
                "Sabah",
                "Sarawak",
                "Selangor",
                "Terengganu",
                // Special
                "Remote",
                "Work From Home",
                "Hybrid"
            };

            // Get locations from database and merge with predefined
            var dbLocations = await _context.JobVacancies
                .Where(j => j.Status == "Open" && !string.IsNullOrEmpty(j.Location))
                .Select(j => j.Location)
                .Distinct()
                .ToListAsync();

            return predefinedLocations
                .Union(dbLocations)
                .Distinct()
                .OrderBy(l => l)
                .ToList();
        }

        private async Task<List<string>> GetDistinctJobTypesAsync()
        {
            // Predefined job types
            var predefinedJobTypes = new List<string>
            {
                "Full-Time",
                "Part-Time",
                "Contract",
                "Internship",
                "Freelance",
                "Temporary"
            };

            // Get job types from database and merge with predefined
            var dbJobTypes = await _context.JobVacancies
                .Where(j => j.Status == "Open" && !string.IsNullOrEmpty(j.JobType))
                .Select(j => j.JobType)
                .Distinct()
                .ToListAsync();

            return predefinedJobTypes
                .Union(dbJobTypes)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
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

        #endregion
    }

    /// <summary>
    /// Request model for bookmark API
    /// </summary>
    public class BookmarkRequest
    {
        public int JobVacancyId { get; set; }
    }
}

