using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    [Authorize(Roles = "Employer,Admin")]
    public class JobVacanciesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobVacanciesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: JobVacancies
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? categoryFilter,
            string? jobTypeFilter,
            string? locationFilter,
            string? statusFilter,
            decimal? minSalary,
            decimal? maxSalary,
            string sortBy = "PublishDate",
            string sortOrder = "desc",
            int pageNumber = 1,
            int pageSize = 10)
        {
            var query = _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .Include(j => j.JobApplications)
                .AsQueryable();

            // For non-authenticated users or job seekers, only show open jobs
            if (!User.Identity?.IsAuthenticated == true || User.IsInRole("JobSeeker"))
            {
                query = query.Where(j => j.Status == "Open");
            }
            // For employers, show only their own jobs
            else if (User.IsInRole("Employer") && !User.IsInRole("Admin"))
            {
                var userId = GetCurrentUserId();
                var employerProfile = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employerProfile != null)
                {
                    query = query.Where(j => j.EmployerProfileId == employerProfile.EmployerProfileId);
                }
            }

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(j => j.JobTitle.Contains(searchTerm) ||
                                         j.Description.Contains(searchTerm) ||
                                         j.EmployerProfile.CompanyName.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                query = query.Where(j => j.Category == categoryFilter);
            }

            if (!string.IsNullOrEmpty(jobTypeFilter))
            {
                query = query.Where(j => j.JobType == jobTypeFilter);
            }

            if (!string.IsNullOrEmpty(locationFilter))
            {
                query = query.Where(j => j.Location.Contains(locationFilter));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(j => j.Status == statusFilter);
            }

            if (minSalary.HasValue)
            {
                query = query.Where(j => j.SalaryMax >= minSalary || j.SalaryMin >= minSalary);
            }

            if (maxSalary.HasValue)
            {
                query = query.Where(j => j.SalaryMin <= maxSalary || j.SalaryMax <= maxSalary);
            }

            // Apply sorting
            query = sortBy switch
            {
                "JobTitle" => sortOrder == "asc" ? query.OrderBy(j => j.JobTitle) : query.OrderByDescending(j => j.JobTitle),
                "CompanyName" => sortOrder == "asc" ? query.OrderBy(j => j.EmployerProfile.CompanyName) : query.OrderByDescending(j => j.EmployerProfile.CompanyName),
                "Location" => sortOrder == "asc" ? query.OrderBy(j => j.Location) : query.OrderByDescending(j => j.Location),
                "SalaryMin" => sortOrder == "asc" ? query.OrderBy(j => j.SalaryMin) : query.OrderByDescending(j => j.SalaryMin),
                "ClosingDate" => sortOrder == "asc" ? query.OrderBy(j => j.ClosingDate) : query.OrderByDescending(j => j.ClosingDate),
                _ => sortOrder == "asc" ? query.OrderBy(j => j.PublishDate) : query.OrderByDescending(j => j.PublishDate)
            };

            var totalCount = await query.CountAsync();

            var jobs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new JobVacancyViewModel
                {
                    JobVacancyId = j.JobVacancyId,
                    EmployerProfileId = j.EmployerProfileId,
                    JobTitle = j.JobTitle,
                    Category = j.Category,
                    JobType = j.JobType,
                    SalaryMin = j.SalaryMin,
                    SalaryMax = j.SalaryMax,
                    Location = j.Location,
                    Description = j.Description,
                    Requirements = j.Requirements,
                    PublishDate = j.PublishDate,
                    ClosingDate = j.ClosingDate,
                    Status = j.Status,
                    CompanyName = j.EmployerProfile.CompanyName,
                    CompanyLogo = j.EmployerProfile.CompanyLogo,
                    ApplicationsCount = j.JobApplications.Count
                })
                .ToListAsync();

            // Get filter options
            var categories = await _context.JobVacancies
                .Select(j => j.Category)
                .Distinct()
                .OrderBy(c => c)
                .Select(c => new SelectListItem(c, c))
                .ToListAsync();

            var locations = await _context.JobVacancies
                .Select(j => j.Location)
                .Distinct()
                .OrderBy(l => l)
                .Select(l => new SelectListItem(l, l))
                .ToListAsync();

            var viewModel = new JobVacancyListViewModel
            {
                Jobs = jobs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                CategoryFilter = categoryFilter,
                JobTypeFilter = jobTypeFilter,
                LocationFilter = locationFilter,
                StatusFilter = statusFilter,
                MinSalary = minSalary,
                MaxSalary = maxSalary,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Categories = categories,
                JobTypes = JobVacancyOptions.GetJobTypes(),
                Locations = locations,
                Statuses = JobVacancyOptions.GetStatuses()
            };

            return View(viewModel);
        }

        // GET: JobVacancies/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .Include(j => j.JobApplications)
                .FirstOrDefaultAsync(j => j.JobVacancyId == id);

            if (job == null)
            {
                return NotFound();
            }

            var viewModel = new JobVacancyViewModel
            {
                JobVacancyId = job.JobVacancyId,
                EmployerProfileId = job.EmployerProfileId,
                JobTitle = job.JobTitle,
                Category = job.Category,
                JobType = job.JobType,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Location = job.Location,
                Description = job.Description,
                Requirements = job.Requirements,
                PublishDate = job.PublishDate,
                ClosingDate = job.ClosingDate,
                Status = job.Status,
                CompanyName = job.EmployerProfile.CompanyName,
                CompanyLogo = job.EmployerProfile.CompanyLogo,
                ApplicationsCount = job.JobApplications.Count
            };

            // Check if current user has already applied
            // This is per-user check: other jobseekers can still apply to the same job
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("JobSeeker"))
            {
                var userId = GetCurrentUserId();
                var jobSeekerProfile = await _context.JobSeekerProfiles
                    .FirstOrDefaultAsync(j => j.UserId == userId);

                if (jobSeekerProfile != null)
                {
                    // Check if THIS user has applied to THIS job with a non-withdrawn status
                    // Withdrawn applications do NOT block re-application
                    ViewBag.HasApplied = await _context.JobApplications
                        .AnyAsync(a => a.JobSeekerProfileId == jobSeekerProfile.JobSeekerProfileId && 
                                       a.JobVacancyId == id &&
                                       a.Status != "Withdrawn");
                    
                    // Check if THIS user has bookmarked THIS job
                    ViewBag.IsBookmarked = await _context.BookmarkedJobs
                        .AnyAsync(b => b.JobSeekerProfileId == jobSeekerProfile.JobSeekerProfileId && 
                                       b.JobVacancyId == id);
                }
            }

            return View(viewModel);
        }

        // GET: JobVacancies/Create
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            var employerProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employerProfile == null)
            {
                TempData["ErrorMessage"] = "Please complete your company profile before posting jobs.";
                return RedirectToAction("Create", "EmployerProfiles");
            }

            var viewModel = new JobVacancyViewModel
            {
                EmployerProfileId = employerProfile.EmployerProfileId,
                PublishDate = DateTime.UtcNow,
                Status = "Draft",
                CategoryOptions = JobVacancyOptions.GetCategories(),
                JobTypeOptions = JobVacancyOptions.GetJobTypes(),
                StatusOptions = JobVacancyOptions.GetStatuses()
            };

            return View(viewModel);
        }

        // POST: JobVacancies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobVacancyViewModel viewModel)
        {
            var userId = GetCurrentUserId();
            var employerProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employerProfile == null)
            {
                return RedirectToAction("Create", "EmployerProfiles");
            }

            viewModel.EmployerProfileId = employerProfile.EmployerProfileId;

            if (ModelState.IsValid)
            {
                var job = new JobVacancy
                {
                    EmployerProfileId = viewModel.EmployerProfileId,
                    JobTitle = viewModel.JobTitle,
                    Category = viewModel.Category,
                    JobType = viewModel.JobType,
                    SalaryMin = viewModel.SalaryMin,
                    SalaryMax = viewModel.SalaryMax,
                    Location = viewModel.Location,
                    Description = viewModel.Description,
                    Requirements = viewModel.Requirements,
                    PublishDate = viewModel.Status == "Open" ? DateTime.UtcNow : viewModel.PublishDate,
                    ClosingDate = viewModel.ClosingDate,
                    Status = viewModel.Status
                };

                _context.Add(job);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job vacancy created successfully!";
                return RedirectToAction(nameof(Details), new { id = job.JobVacancyId });
            }

            viewModel.CategoryOptions = JobVacancyOptions.GetCategories();
            viewModel.JobTypeOptions = JobVacancyOptions.GetJobTypes();
            viewModel.StatusOptions = JobVacancyOptions.GetStatuses();

            return View(viewModel);
        }

        // GET: JobVacancies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .FirstOrDefaultAsync(j => j.JobVacancyId == id);

            if (job == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin") && job.EmployerProfile.UserId != userId)
            {
                return Forbid();
            }

            var viewModel = new JobVacancyViewModel
            {
                JobVacancyId = job.JobVacancyId,
                EmployerProfileId = job.EmployerProfileId,
                JobTitle = job.JobTitle,
                Category = job.Category,
                JobType = job.JobType,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Location = job.Location,
                Description = job.Description,
                Requirements = job.Requirements,
                PublishDate = job.PublishDate,
                ClosingDate = job.ClosingDate,
                Status = job.Status,
                CategoryOptions = JobVacancyOptions.GetCategories(),
                JobTypeOptions = JobVacancyOptions.GetJobTypes(),
                StatusOptions = JobVacancyOptions.GetStatuses()
            };

            return View(viewModel);
        }

        // POST: JobVacancies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobVacancyViewModel viewModel)
        {
            if (id != viewModel.JobVacancyId)
            {
                return NotFound();
            }

            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .FirstOrDefaultAsync(j => j.JobVacancyId == id);

            if (job == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin") && job.EmployerProfile.UserId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    job.JobTitle = viewModel.JobTitle;
                    job.Category = viewModel.Category;
                    job.JobType = viewModel.JobType;
                    job.SalaryMin = viewModel.SalaryMin;
                    job.SalaryMax = viewModel.SalaryMax;
                    job.Location = viewModel.Location;
                    job.Description = viewModel.Description;
                    job.Requirements = viewModel.Requirements;
                    job.ClosingDate = viewModel.ClosingDate;
                    job.Status = viewModel.Status;

                    _context.Update(job);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Job vacancy updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await JobExists(id))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return RedirectToAction(nameof(Details), new { id });
            }

            viewModel.CategoryOptions = JobVacancyOptions.GetCategories();
            viewModel.JobTypeOptions = JobVacancyOptions.GetJobTypes();
            viewModel.StatusOptions = JobVacancyOptions.GetStatuses();

            return View(viewModel);
        }

        // GET: JobVacancies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .Include(j => j.JobApplications)
                .FirstOrDefaultAsync(j => j.JobVacancyId == id);

            if (job == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin") && job.EmployerProfile.UserId != userId)
            {
                return Forbid();
            }

            var viewModel = new JobVacancyViewModel
            {
                JobVacancyId = job.JobVacancyId,
                JobTitle = job.JobTitle,
                Category = job.Category,
                JobType = job.JobType,
                Location = job.Location,
                Status = job.Status,
                CompanyName = job.EmployerProfile.CompanyName,
                ApplicationsCount = job.JobApplications.Count
            };

            return View(viewModel);
        }

        // POST: JobVacancies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .FirstOrDefaultAsync(j => j.JobVacancyId == id);

            if (job == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin") && job.EmployerProfile.UserId != userId)
            {
                return Forbid();
            }

            _context.JobVacancies.Remove(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job vacancy deleted successfully!";
            return RedirectToAction(nameof(Index));
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

        private async Task<bool> JobExists(int id)
        {
            return await _context.JobVacancies.AnyAsync(j => j.JobVacancyId == id);
        }

        #endregion
    }
}

