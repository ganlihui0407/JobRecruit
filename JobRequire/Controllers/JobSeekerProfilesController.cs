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
    [Authorize(Roles = "JobSeeker,Admin")]
    public class JobSeekerProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IResumeCompletionService _completionService;
        private readonly IConfiguration _configuration;

        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }
        public JobSeekerProfilesController(
            ApplicationDbContext context, 
            IWebHostEnvironment environment,
            IResumeCompletionService completionService,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _completionService = completionService;
            _configuration = configuration;
        }

        // GET: JobSeekerProfiles/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Load profile with all necessary includes
            var profile = await _context.JobSeekerProfiles
                .Include(j => j.User)
                .Include(j => j.ResumeEducations)
                .Include(j => j.ResumeExperiences)
                .Include(j => j.ResumeSkills)
                .Include(j => j.JobApplications)
                    .ThenInclude(a => a.JobVacancy)
                        .ThenInclude(v => v!.EmployerProfile)
                .Include(j => j.BookmarkedJobs)
                    .ThenInclude(b => b.JobVacancy)
                        .ThenInclude(v => v!.EmployerProfile)
                .FirstOrDefaultAsync(j => j.UserId == userId);

            // If profile doesn't exist, try to auto-create a minimal one or redirect
            if (profile == null)
            {
                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Auto-create a minimal profile with user's email as default name
                try
                {
                    profile = new JobSeekerProfile
                    {
                        UserId = userId.Value,
                        FullName = user.Email.Split('@')[0], // Use email prefix as default name
                        PhoneNumber = null,
                        Summary = null,
                        ProfilePhoto = null
                    };
                    _context.JobSeekerProfiles.Add(profile);
                    await _context.SaveChangesAsync();

                    // Reload with includes
                    profile = await _context.JobSeekerProfiles
                        .Include(j => j.User)
                        .Include(j => j.ResumeEducations)
                        .Include(j => j.ResumeExperiences)
                        .Include(j => j.ResumeSkills)
                        .Include(j => j.JobApplications)
                            .ThenInclude(a => a.JobVacancy)
                                .ThenInclude(v => v!.EmployerProfile)
                        .Include(j => j.BookmarkedJobs)
                            .ThenInclude(b => b.JobVacancy)
                                .ThenInclude(v => v!.EmployerProfile)
                        .FirstOrDefaultAsync(j => j.JobSeekerProfileId == profile.JobSeekerProfileId);

                    if (profile == null)
                    {
                        TempData["ErrorMessage"] = "Failed to create profile. Please try again.";
                        return RedirectToAction(nameof(Create));
                    }

                    TempData["InfoMessage"] = "Profile created. Please complete your profile information.";
                }
                catch (Exception)
                {
                    // If auto-creation fails, redirect to Create
                    return RedirectToAction(nameof(Create));
                }
            }

            // Validate required fields - redirect to Edit if profile is incomplete
            if (string.IsNullOrWhiteSpace(profile.FullName) || profile.User == null)
            {
                TempData["WarningMessage"] = "Please complete your profile to access the dashboard.";
                if (profile.JobSeekerProfileId > 0)
                {
                    return RedirectToAction(nameof(Edit), new { id = profile.JobSeekerProfileId });
                }
                return RedirectToAction(nameof(Create));
            }

            // Safely get collections (defensive null checks)
            var educations = profile.ResumeEducations ?? new List<ResumeEducation>();
            var experiences = profile.ResumeExperiences ?? new List<ResumeExperience>();
            var skills = profile.ResumeSkills ?? new List<ResumeSkill>();
            var applications = profile.JobApplications ?? new List<JobApplication>();
            var bookmarks = profile.BookmarkedJobs ?? new List<BookmarkedJob>();

            // Calculate profile completion using the shared service
            var profileCompletion = _completionService.CalculateCompletion(profile);

            // Build view model with defensive null handling
            var viewModel = new JobSeekerDashboardViewModel
            {
                Profile = new JobSeekerProfileViewModel
                {
                    JobSeekerProfileId = profile.JobSeekerProfileId,
                    UserId = profile.UserId,
                    FullName = profile.FullName ?? string.Empty, // Defensive: should never be null after validation
                    PhoneNumber = profile.PhoneNumber ?? string.Empty,
                    Summary = profile.Summary ?? string.Empty,
                    ProfilePhoto = profile.ProfilePhoto ?? string.Empty,
                    Username = profile.User?.Username ?? string.Empty,
                    Email = profile.User?.Email ?? string.Empty,
                    Education = educations.Select(e => new ResumeEducationViewModel
                    {
                        ResumeEducationId = e.ResumeEducationId,
                        SchoolName = e.SchoolName ?? string.Empty,
                        Qualification = e.Qualification ?? string.Empty,
                        FieldOfStudy = e.FieldOfStudy,
                        StartYear = e.StartYear,
                        EndYear = e.EndYear
                    }).ToList(),
                    Experience = experiences.Select(e => new ResumeExperienceViewModel
                    {
                        ResumeExperienceId = e.ResumeExperienceId,
                        CompanyName = e.CompanyName ?? string.Empty,
                        JobTitle = e.JobTitle ?? string.Empty,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Description = e.Description
                    }).ToList(),
                    Skills = skills.Select(s => new ResumeSkillViewModel
                    {
                        ResumeSkillId = s.ResumeSkillId,
                        SkillName = s.SkillName ?? string.Empty,
                        SkillLevel = s.SkillLevel ?? "Intermediate"
                    }).ToList()
                },
                RecentApplications = applications
                    .Where(a => a != null && a.JobVacancy != null && a.JobVacancy.EmployerProfile != null)
                    .OrderByDescending(a => a.ApplyDate)
                    .Take(5)
                    .Select(a => new JobApplicationViewModel
                    {
                        JobApplicationId = a.JobApplicationId,
                        JobVacancyId = a.JobVacancyId,
                        ApplyDate = a.ApplyDate,
                        Status = a.Status ?? "Pending",
                        JobTitle = a.JobVacancy!.JobTitle ?? "Unknown Job",
                        CompanyName = a.JobVacancy.EmployerProfile!.CompanyName ?? "Unknown Company"
                    }).ToList(),
                RecentBookmarks = bookmarks
                    .Where(b => b != null && b.JobVacancy != null && b.JobVacancy.EmployerProfile != null)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new BookmarkedJobViewModel
                    {
                        BookmarkedJobId = b.BookmarkedJobId,
                        JobVacancyId = b.JobVacancyId,
                        CreatedAt = b.CreatedAt,
                        JobTitle = b.JobVacancy!.JobTitle ?? "Unknown Job",
                        CompanyName = b.JobVacancy.EmployerProfile!.CompanyName ?? "Unknown Company",
                        Location = b.JobVacancy.Location ?? "Not specified",
                        Status = b.JobVacancy.Status ?? "Unknown"
                    }).ToList(),
                TotalApplications = applications.Count,
                PendingApplications = applications.Count(a => a.Status == "Pending"),
                ShortlistedApplications = applications.Count(a => a.Status == "Shortlisted" || a.Status == "Interview"),
                RejectedApplications = applications.Count(a => a.Status == "Rejected"),
                ProfileCompletion = profileCompletion,
                // Generate public profile identifier using the shared ProfileController method
                PublicIdentifier = ProfileController.GeneratePublicIdentifier(profile.JobSeekerProfileId),
                // Generate full URL for public profile sharing using configurable base URL
                PublicProfileUrl = GetPublicProfileUrl(profile.JobSeekerProfileId)
            };

            return View(viewModel);
        }

        // GET: JobSeekerProfiles/Create
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Check if profile already exists
            var existingProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == userId);

            if (existingProfile != null)
            {
                return RedirectToAction(nameof(Edit), new { id = existingProfile.JobSeekerProfileId });
            }

            var user = await _context.Users.FindAsync(userId);

            return View(new JobSeekerProfileViewModel
            {
                UserId = userId.Value,
                Email = user?.Email
            });
        }

        // POST: JobSeekerProfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobSeekerProfileViewModel viewModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            viewModel.UserId = userId.Value;

            if (ModelState.IsValid)
            {
                var profile = new JobSeekerProfile
                {
                    UserId = viewModel.UserId,
                    FullName = viewModel.FullName,
                    PhoneNumber = viewModel.PhoneNumber,
                    Summary = viewModel.Summary
                };

                // Handle file upload
                if (viewModel.ProfilePhotoFile != null && viewModel.ProfilePhotoFile.Length > 0)
                {
                    profile.ProfilePhoto = await SaveUploadedFile(viewModel.ProfilePhotoFile, "photos");
                }

                _context.Add(profile);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile created successfully! Now add your education, experience, and skills.";
                return RedirectToAction(nameof(Edit), new { id = profile.JobSeekerProfileId });
            }

            return View(viewModel);
        }

        // GET: JobSeekerProfiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            JobSeekerProfile? profile;

            if (id == null)
            {
                profile = await _context.JobSeekerProfiles
                    .Include(j => j.User)
                    .Include(j => j.ResumeEducations)
                    .Include(j => j.ResumeExperiences)
                    .Include(j => j.ResumeSkills)
                    .FirstOrDefaultAsync(j => j.UserId == userId);
            }
            else
            {
                profile = await _context.JobSeekerProfiles
                    .Include(j => j.User)
                    .Include(j => j.ResumeEducations)
                    .Include(j => j.ResumeExperiences)
                    .Include(j => j.ResumeSkills)
                    .FirstOrDefaultAsync(j => j.JobSeekerProfileId == id);
            }

            if (profile == null)
            {
                return RedirectToAction(nameof(Create));
            }

            // Check authorization
            if (!User.IsInRole("Admin") && profile.UserId != userId)
            {
                return Forbid();
            }

            var viewModel = new JobSeekerProfileViewModel
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                UserId = profile.UserId,
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Summary = profile.Summary,
                ProfilePhoto = profile.ProfilePhoto,
                Username = profile.User.Username,
                Email = profile.User.Email,
                Education = profile.ResumeEducations
                    .OrderByDescending(e => e.EndYear)
                    .Select(e => new ResumeEducationViewModel
                    {
                        ResumeEducationId = e.ResumeEducationId,
                        JobSeekerProfileId = e.JobSeekerProfileId,
                        SchoolName = e.SchoolName,
                        Qualification = e.Qualification,
                        FieldOfStudy = e.FieldOfStudy,
                        StartYear = e.StartYear,
                        EndYear = e.EndYear,
                        QualificationOptions = ResumeOptions.GetQualifications()
                    }).ToList(),
                Experience = profile.ResumeExperiences
                    .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                    .Select(e => new ResumeExperienceViewModel
                    {
                        ResumeExperienceId = e.ResumeExperienceId,
                        JobSeekerProfileId = e.JobSeekerProfileId,
                        CompanyName = e.CompanyName,
                        JobTitle = e.JobTitle,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Description = e.Description,
                        IsCurrentJob = !e.EndDate.HasValue
                    }).ToList(),
                Skills = profile.ResumeSkills
                    .Select(s => new ResumeSkillViewModel
                    {
                        ResumeSkillId = s.ResumeSkillId,
                        JobSeekerProfileId = s.JobSeekerProfileId,
                        SkillName = s.SkillName,
                        SkillLevel = s.SkillLevel,
                        SkillLevelOptions = ResumeOptions.GetSkillLevels()
                    }).ToList()
            };

            return View(viewModel);
        }

        // POST: JobSeekerProfiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobSeekerProfileViewModel viewModel)
        {
            if (id != viewModel.JobSeekerProfileId)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var profile = await _context.JobSeekerProfiles.FindAsync(id);

            if (profile == null)
            {
                return NotFound();
            }

            // Check authorization
            if (!User.IsInRole("Admin") && profile.UserId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    profile.FullName = viewModel.FullName;
                    profile.PhoneNumber = viewModel.PhoneNumber;
                    profile.Summary = viewModel.Summary;

                    // Handle file upload
                    if (viewModel.ProfilePhotoFile != null && viewModel.ProfilePhotoFile.Length > 0)
                    {
                        // Delete old file
                        if (!string.IsNullOrEmpty(profile.ProfilePhoto))
                        {
                            DeleteFile(profile.ProfilePhoto);
                        }

                        profile.ProfilePhoto = await SaveUploadedFile(viewModel.ProfilePhotoFile, "photos");
                    }

                    _context.Update(profile);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProfileExists(id))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return RedirectToAction(nameof(Edit), new { id });
            }

            return View(viewModel);
        }

        // GET: JobSeekerProfiles/MyProfile
        public async Task<IActionResult> MyProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == userId);

            if (profile == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Edit), new { id = profile.JobSeekerProfileId });
        }

        // GET: JobSeekerProfiles/Bookmarks
        [Authorize(Roles = "JobSeeker,Admin")]
        public async Task<IActionResult> Bookmarks(int pageNumber = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == userId);

            if (profile == null)
            {
                TempData["WarningMessage"] = "Please complete your profile first.";
                return RedirectToAction(nameof(Create));
            }

            var query = _context.BookmarkedJobs
                .Include(b => b.JobVacancy)
                    .ThenInclude(j => j!.EmployerProfile)
                .Where(b => b.JobSeekerProfileId == profile.JobSeekerProfileId);

            var totalCount = await query.CountAsync();

            var bookmarks = await query
                .Where(b => b.JobVacancy != null && b.JobVacancy.EmployerProfile != null)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookmarkedJobViewModel
                {
                    BookmarkedJobId = b.BookmarkedJobId,
                    JobSeekerProfileId = b.JobSeekerProfileId,
                    JobVacancyId = b.JobVacancyId,
                    CreatedAt = b.CreatedAt,
                    JobTitle = b.JobVacancy!.JobTitle ?? "Unknown Job",
                    CompanyName = b.JobVacancy.EmployerProfile!.CompanyName ?? "Unknown Company",
                    Location = b.JobVacancy.Location ?? "Not specified",
                    JobType = b.JobVacancy.JobType ?? "Full-time",
                    SalaryMin = b.JobVacancy.SalaryMin,
                    SalaryMax = b.JobVacancy.SalaryMax,
                    Status = b.JobVacancy.Status ?? "Unknown"
                })
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return View(bookmarks);
        }

        // POST: JobSeekerProfiles/ToggleBookmark
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBookmark(int jobVacancyId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == userId);

            if (profile == null)
            {
                return Json(new { success = false, message = "Profile not found" });
            }

            var existingBookmark = await _context.BookmarkedJobs
                .FirstOrDefaultAsync(b => b.JobSeekerProfileId == profile.JobSeekerProfileId &&
                                          b.JobVacancyId == jobVacancyId);

            if (existingBookmark != null)
            {
                _context.BookmarkedJobs.Remove(existingBookmark);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isBookmarked = false, message = "Bookmark removed" });
            }
            else
            {
                var bookmark = new BookmarkedJob
                {
                    JobSeekerProfileId = profile.JobSeekerProfileId,
                    JobVacancyId = jobVacancyId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Add(bookmark);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isBookmarked = true, message = "Job bookmarked" });
            }
        }

        // POST: JobSeekerProfiles/RemoveBookmark
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBookmark(int id)
        {
            var userId = GetCurrentUserId();
            var bookmark = await _context.BookmarkedJobs
                .Include(b => b.JobSeekerProfile)
                .FirstOrDefaultAsync(b => b.BookmarkedJobId == id);

            if (bookmark == null)
            {
                return NotFound();
            }

            if (bookmark.JobSeekerProfile.UserId != userId)
            {
                return Forbid();
            }

            _context.BookmarkedJobs.Remove(bookmark);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bookmark removed.";
            return RedirectToAction(nameof(Bookmarks));
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

        private async Task<bool> ProfileExists(int id)
        {
            return await _context.JobSeekerProfiles.AnyAsync(j => j.JobSeekerProfileId == id);
        }

        private async Task<string> SaveUploadedFile(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{uniqueFileName}";
        }

        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        /// <summary>
        /// Generate public profile URL using configurable base URL.
        /// If PublicProfile:BaseUrl is set in appsettings.json, use that.
        /// Otherwise, fall back to the current request's host.
        /// </summary>
        private string GetPublicProfileUrl(int profileId)
        {
            var identifier = ProfileController.GeneratePublicIdentifier(profileId);
            var configuredBaseUrl = _configuration["PublicProfile:BaseUrl"];
            
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                // Use configured public base URL (e.g., https://mydomain.com or ngrok URL)
                var baseUrl = configuredBaseUrl.TrimEnd('/');
                return $"{baseUrl}/Profile/Public/{identifier}";
            }
            
            // Fall back to current request host (for local development)
            return $"{Request.Scheme}://{Request.Host}/Profile/Public/{identifier}";
        }

        #endregion
    }
}

