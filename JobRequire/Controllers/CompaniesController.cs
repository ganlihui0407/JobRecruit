using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Services;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Public Employer Directory controller - accessible to all users (guests, job seekers, employers)
    /// </summary>
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompaniesController> _logger;
        private readonly IJobInterestService _interestService;

        public CompaniesController(
            ApplicationDbContext context, 
            ILogger<CompaniesController> logger,
            IJobInterestService interestService)
        {
            _context = context;
            _logger = logger;
            _interestService = interestService;
        }

        /// <summary>
        /// GET: Companies - Public employer directory listing
        /// Accessible to: Guests, JobSeekers, Employers, Admins
        /// </summary>
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? industryFilter,
            int pageNumber = 1,
            int pageSize = 12)
        {
            var query = _context.EmployerProfiles
                .Include(e => e.JobVacancies)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(e => 
                    e.CompanyName.ToLower().Contains(searchLower) ||
                    (e.CompanyDescription != null && e.CompanyDescription.ToLower().Contains(searchLower)) ||
                    (e.Industry != null && e.Industry.ToLower().Contains(searchLower)));
            }

            // Apply industry filter
            if (!string.IsNullOrEmpty(industryFilter))
            {
                query = query.Where(e => e.Industry == industryFilter);
            }

            var totalCount = await query.CountAsync();

            // Get companies with open job count
            var companies = await query
                .OrderByDescending(e => e.JobVacancies.Count(j => j.Status == "Open"))
                .ThenBy(e => e.CompanyName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new CompanyViewModel
                {
                    EmployerProfileId = e.EmployerProfileId,
                    CompanyName = e.CompanyName,
                    CompanyLogo = e.CompanyLogo,
                    Industry = e.Industry,
                    CompanyDescription = e.CompanyDescription,
                    CompanyAddress = e.CompanyAddress,
                    OpenJobsCount = e.JobVacancies.Count(j => j.Status == "Open")
                })
                .ToListAsync();

            // Get distinct industries for filter dropdown
            var industries = await _context.EmployerProfiles
                .Where(e => e.Industry != null && e.Industry != "")
                .Select(e => e.Industry!)
                .Distinct()
                .OrderBy(i => i)
                .ToListAsync();

            var viewModel = new CompanyListViewModel
            {
                Companies = companies,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                IndustryFilter = industryFilter,
                Industries = industries
            };

            return View(viewModel);
        }

        /// <summary>
        /// GET: Companies/Details/{id} - Company profile with open job listings
        /// Accessible to: Guests, JobSeekers, Employers, Admins
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.EmployerProfiles
                .Include(e => e.JobVacancies)
                    .ThenInclude(j => j.JobInterests)
                .FirstOrDefaultAsync(e => e.EmployerProfileId == id);

            if (company == null)
            {
                return NotFound();
            }

            // Get open job listings for this company, sorted by popularity (InterestedCount) DESC
            var openJobs = company.JobVacancies
                .Where(j => j.Status == "Open")
                .OrderByDescending(j => j.JobInterests.Count)
                .ThenByDescending(j => j.PublishDate)
                .Select(j => new CompanyJobListingViewModel
                {
                    JobVacancyId = j.JobVacancyId,
                    JobTitle = j.JobTitle,
                    Location = j.Location,
                    JobType = j.JobType,
                    Category = j.Category,
                    SalaryMin = j.SalaryMin,
                    SalaryMax = j.SalaryMax,
                    PublishDate = j.PublishDate,
                    ClosingDate = j.ClosingDate,
                    InterestedCount = j.JobInterests.Count
                })
                .ToList();

            // Check if current user is the owner of this company
            var currentUserId = GetCurrentUserId();
            var isOwnCompany = currentUserId.HasValue && company.UserId == currentUserId.Value;
            var canManage = isOwnCompany || User.IsInRole("Admin");

            var viewModel = new CompanyDetailsViewModel
            {
                EmployerProfileId = company.EmployerProfileId,
                CompanyName = company.CompanyName,
                CompanyLogo = company.CompanyLogo,
                Industry = company.Industry,
                CompanyDescription = company.CompanyDescription,
                CompanyAddress = company.CompanyAddress,
                OpenJobsCount = openJobs.Count,
                OpenJobs = openJobs,
                IsOwnCompany = isOwnCompany,
                CanManage = canManage
            };

            return View(viewModel);
        }

        #region Helper Methods

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
}


