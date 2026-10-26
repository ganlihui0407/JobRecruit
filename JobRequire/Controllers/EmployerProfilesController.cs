using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    [Authorize(Roles = "Employer,Admin")]
    public class EmployerProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EmployerProfilesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: EmployerProfiles
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchTerm, string? industryFilter, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.EmployerProfiles
                .Include(e => e.User)
                .Include(e => e.JobVacancies)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(e => e.CompanyName.Contains(searchTerm) || 
                                         (e.CompanyDescription != null && e.CompanyDescription.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(industryFilter))
            {
                query = query.Where(e => e.Industry == industryFilter);
            }

            var totalCount = await query.CountAsync();

            var profiles = await query
                .OrderBy(e => e.CompanyName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployerProfileViewModel
                {
                    EmployerProfileId = e.EmployerProfileId,
                    UserId = e.UserId,
                    CompanyName = e.CompanyName,
                    CompanyAddress = e.CompanyAddress,
                    Industry = e.Industry,
                    CompanyLogo = e.CompanyLogo,
                    CompanyDescription = e.CompanyDescription,
                    Username = e.User.Username,
                    Email = e.User.Email,
                    JobVacanciesCount = e.JobVacancies.Count(j => j.Status == "Open")
                })
                .ToListAsync();

            var industries = await _context.EmployerProfiles
                .Where(e => e.Industry != null)
                .Select(e => e.Industry!)
                .Distinct()
                .OrderBy(i => i)
                .ToListAsync();

            var viewModel = new EmployerProfileListViewModel
            {
                Profiles = profiles,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                IndustryFilter = industryFilter,
                Industries = industries
            };

            return View(viewModel);
        }

        // GET: EmployerProfiles/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.EmployerProfiles
                .Include(e => e.User)
                .Include(e => e.JobVacancies)
                .FirstOrDefaultAsync(e => e.EmployerProfileId == id);

            if (profile == null)
            {
                return NotFound();
            }

            var viewModel = new EmployerProfileViewModel
            {
                EmployerProfileId = profile.EmployerProfileId,
                UserId = profile.UserId,
                CompanyName = profile.CompanyName,
                CompanyAddress = profile.CompanyAddress,
                Industry = profile.Industry,
                CompanyLogo = profile.CompanyLogo,
                CompanyDescription = profile.CompanyDescription,
                Username = profile.User.Username,
                Email = profile.User.Email,
                JobVacanciesCount = profile.JobVacancies.Count(j => j.Status == "Open")
            };

            return View(viewModel);
        }

        // GET: EmployerProfiles/Create
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Check if profile already exists
            var existingProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (existingProfile != null)
            {
                return RedirectToAction(nameof(Edit), new { id = existingProfile.EmployerProfileId });
            }

            return View(new EmployerProfileViewModel { UserId = userId.Value });
        }

        // POST: EmployerProfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployerProfileViewModel viewModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            viewModel.UserId = userId.Value;

            if (ModelState.IsValid)
            {
                var profile = new EmployerProfile
                {
                    UserId = viewModel.UserId,
                    CompanyName = viewModel.CompanyName,
                    CompanyAddress = viewModel.CompanyAddress,
                    Industry = viewModel.Industry,
                    CompanyDescription = viewModel.CompanyDescription
                };

                // Handle file upload
                if (viewModel.CompanyLogoFile != null && viewModel.CompanyLogoFile.Length > 0)
                {
                    profile.CompanyLogo = await SaveUploadedFile(viewModel.CompanyLogoFile, "logos");
                }

                _context.Add(profile);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Company profile created successfully!";
                return RedirectToAction(nameof(Details), new { id = profile.EmployerProfileId });
            }

            return View(viewModel);
        }

        // GET: EmployerProfiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var profile = await _context.EmployerProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployerProfileId == id);

            if (profile == null)
            {
                return NotFound();
            }

            // Check authorization
            if (!User.IsInRole("Admin") && profile.UserId != userId)
            {
                return Forbid();
            }

            var viewModel = new EmployerProfileViewModel
            {
                EmployerProfileId = profile.EmployerProfileId,
                UserId = profile.UserId,
                CompanyName = profile.CompanyName,
                CompanyAddress = profile.CompanyAddress,
                Industry = profile.Industry,
                CompanyLogo = profile.CompanyLogo,
                CompanyDescription = profile.CompanyDescription,
                Username = profile.User.Username,
                Email = profile.User.Email
            };

            return View(viewModel);
        }

        // POST: EmployerProfiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployerProfileViewModel viewModel)
        {
            if (id != viewModel.EmployerProfileId)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var profile = await _context.EmployerProfiles.FindAsync(id);

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
                    profile.CompanyName = viewModel.CompanyName;
                    profile.CompanyAddress = viewModel.CompanyAddress;
                    profile.Industry = viewModel.Industry;
                    profile.CompanyDescription = viewModel.CompanyDescription;

                    // Handle file upload
                    if (viewModel.CompanyLogoFile != null && viewModel.CompanyLogoFile.Length > 0)
                    {
                        // Delete old file
                        if (!string.IsNullOrEmpty(profile.CompanyLogo))
                        {
                            DeleteFile(profile.CompanyLogo);
                        }

                        profile.CompanyLogo = await SaveUploadedFile(viewModel.CompanyLogoFile, "logos");
                    }

                    _context.Update(profile);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Company profile updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProfileExists(viewModel.EmployerProfileId))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return RedirectToAction(nameof(Details), new { id = viewModel.EmployerProfileId });
            }

            return View(viewModel);
        }

        // GET: EmployerProfiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var profile = await _context.EmployerProfiles
                .Include(e => e.User)
                .Include(e => e.JobVacancies)
                .FirstOrDefaultAsync(e => e.EmployerProfileId == id);

            if (profile == null)
            {
                return NotFound();
            }

            // Check authorization
            if (!User.IsInRole("Admin") && profile.UserId != userId)
            {
                return Forbid();
            }

            var viewModel = new EmployerProfileViewModel
            {
                EmployerProfileId = profile.EmployerProfileId,
                UserId = profile.UserId,
                CompanyName = profile.CompanyName,
                CompanyAddress = profile.CompanyAddress,
                Industry = profile.Industry,
                CompanyLogo = profile.CompanyLogo,
                CompanyDescription = profile.CompanyDescription,
                JobVacanciesCount = profile.JobVacancies.Count
            };

            return View(viewModel);
        }

        // POST: EmployerProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            var profile = await _context.EmployerProfiles.FindAsync(id);

            if (profile == null)
            {
                return NotFound();
            }

            // Check authorization
            if (!User.IsInRole("Admin") && profile.UserId != userId)
            {
                return Forbid();
            }

            // Delete logo file
            if (!string.IsNullOrEmpty(profile.CompanyLogo))
            {
                DeleteFile(profile.CompanyLogo);
            }

            _context.EmployerProfiles.Remove(profile);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Company profile deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: EmployerProfiles/MyProfile
        public async Task<IActionResult> MyProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Edit), new { id = profile.EmployerProfileId });
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
            return await _context.EmployerProfiles.AnyAsync(e => e.EmployerProfileId == id);
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

        #endregion
    }
}

