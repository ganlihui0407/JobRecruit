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
    public class ResumeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IResumeService _resumeService;
        private readonly IPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ResumeController> _logger;

        public ResumeController(
            ApplicationDbContext context,
            IResumeService resumeService,
            IPdfService pdfService,
            IWebHostEnvironment environment,
            ILogger<ResumeController> logger)
        {
            _context = context;
            _resumeService = resumeService;
            _pdfService = pdfService;
            _environment = environment;
            _logger = logger;
        }

        #region Dashboard

        /// <summary>
        /// GET: Resume - Main dashboard with tabs
        /// </summary>
        public async Task<IActionResult> Index(string? tab)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return RedirectToAction("Create", "JobSeekerProfiles");

            var viewModel = await BuildDashboardViewModelAsync(profile);
            ViewBag.ActiveTab = tab ?? "profile";

            return View(viewModel);
        }

        #endregion

        #region Profile

        /// <summary>
        /// GET: Resume/EditProfile
        /// </summary>
        public async Task<IActionResult> EditProfile()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return RedirectToAction("Create", "JobSeekerProfiles");

            var viewModel = new ResumeProfileViewModel
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Summary = profile.Summary,
                ProfilePhoto = profile.ProfilePhoto
            };

            return PartialView("_EditProfilePartial", viewModel);
        }

        /// <summary>
        /// POST: Resume/UpdateProfile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ResumeProfileViewModel model)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new ResumeApiResponse { Success = false, Message = string.Join(", ", errors) });
            }

            try
            {
                profile.FullName = model.FullName;
                profile.PhoneNumber = model.PhoneNumber;
                profile.Summary = model.Summary;
                // ResumeLastUpdated property removed from model

                // Handle photo upload
                if (model.ProfilePhotoFile != null && model.ProfilePhotoFile.Length > 0)
                {
                    // Delete old photo
                    if (!string.IsNullOrEmpty(profile.ProfilePhoto))
                    {
                        DeleteFile(profile.ProfilePhoto);
                    }
                    profile.ProfilePhoto = await SaveUploadedFileAsync(model.ProfilePhotoFile, "photos");
                }

                await _context.SaveChangesAsync();

                return Json(new ResumeApiResponse
                {
                    Success = true,
                    Message = "Profile updated successfully.",
                    Data = new { profilePhoto = profile.ProfilePhoto }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return Json(new ResumeApiResponse { Success = false, Message = "An error occurred while updating profile." });
            }
        }

        #endregion

        #region Education CRUD

        /// <summary>
        /// GET: Resume/EducationList
        /// </summary>
        public async Task<IActionResult> EducationList()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return PartialView("_EducationListPartial", new List<ResumeEducationViewModel>());

            var education = await _context.ResumeEducations
                .Where(e => e.JobSeekerProfileId == profile.JobSeekerProfileId)
                .OrderByDescending(e => e.EndYear ?? 9999)
                .ThenByDescending(e => e.StartYear)
                .Select(e => new ResumeEducationViewModel
                {
                    ResumeEducationId = e.ResumeEducationId,
                    JobSeekerProfileId = e.JobSeekerProfileId,
                    SchoolName = e.SchoolName,
                    Qualification = e.Qualification,
                    FieldOfStudy = e.FieldOfStudy,
                    StartYear = e.StartYear,
                    EndYear = e.EndYear,
                    IsCurrentlyStudying = !e.EndYear.HasValue
                })
                .ToListAsync();

            return PartialView("_EducationListPartial", education);
        }

        /// <summary>
        /// GET: Resume/AddEducation
        /// </summary>
        public async Task<IActionResult> AddEducation()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var viewModel = new ResumeEducationViewModel
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                StartYear = DateTime.Now.Year - 4,
                QualificationOptions = ResumeOptions.GetQualifications()
            };

            return PartialView("_EducationFormPartial", viewModel);
        }

        /// <summary>
        /// GET: Resume/EditEducation/5
        /// </summary>
        public async Task<IActionResult> EditEducation(int id)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var education = await _context.ResumeEducations
                .FirstOrDefaultAsync(e => e.ResumeEducationId == id && e.JobSeekerProfileId == profile.JobSeekerProfileId);

            if (education == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Education record not found." });

            var viewModel = new ResumeEducationViewModel
            {
                ResumeEducationId = education.ResumeEducationId,
                JobSeekerProfileId = education.JobSeekerProfileId,
                SchoolName = education.SchoolName,
                Qualification = education.Qualification,
                FieldOfStudy = education.FieldOfStudy,
                StartYear = education.StartYear,
                EndYear = education.EndYear,
                IsCurrentlyStudying = !education.EndYear.HasValue,
                QualificationOptions = ResumeOptions.GetQualifications()
            };

            return PartialView("_EducationFormPartial", viewModel);
        }

        /// <summary>
        /// POST: Resume/SaveEducation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEducation(ResumeEducationViewModel model)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new ResumeApiResponse { Success = false, Message = string.Join(", ", errors) });
            }

            try
            {
                if (model.ResumeEducationId > 0)
                {
                    // Update existing
                    var education = await _context.ResumeEducations
                        .FirstOrDefaultAsync(e => e.ResumeEducationId == model.ResumeEducationId &&
                                                  e.JobSeekerProfileId == profile.JobSeekerProfileId);

                    if (education == null)
                        return Json(new ResumeApiResponse { Success = false, Message = "Education record not found." });

                    education.SchoolName = model.SchoolName;
                    education.Qualification = model.Qualification;
                    education.FieldOfStudy = model.FieldOfStudy;
                    education.StartYear = model.StartYear;
                    education.EndYear = model.IsCurrentlyStudying ? null : model.EndYear;
                }
                else
                {
                    // Create new
                    var education = new ResumeEducation
                    {
                        JobSeekerProfileId = profile.JobSeekerProfileId,
                        SchoolName = model.SchoolName,
                        Qualification = model.Qualification,
                        FieldOfStudy = model.FieldOfStudy,
                        StartYear = model.StartYear,
                        EndYear = model.IsCurrentlyStudying ? null : model.EndYear
                    };
                    _context.ResumeEducations.Add(education);
                }

                // ResumeLastUpdated property removed from model
                await _context.SaveChangesAsync();

                return Json(new ResumeApiResponse { Success = true, Message = "Education saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving education");
                return Json(new ResumeApiResponse { Success = false, Message = "An error occurred while saving education." });
            }
        }

        /// <summary>
        /// POST: Resume/DeleteEducation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int id)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var education = await _context.ResumeEducations
                .FirstOrDefaultAsync(e => e.ResumeEducationId == id && e.JobSeekerProfileId == profile.JobSeekerProfileId);

            if (education == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Education record not found." });

            try
            {
                _context.ResumeEducations.Remove(education);
                // ResumeLastUpdated property removed from model
                await _context.SaveChangesAsync();

                return Json(new ResumeApiResponse { Success = true, Message = "Education deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting education");
                return Json(new ResumeApiResponse { Success = false, Message = "An error occurred while deleting education." });
            }
        }

        #endregion

        #region Experience CRUD

        /// <summary>
        /// GET: Resume/ExperienceList
        /// </summary>
        public async Task<IActionResult> ExperienceList()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return PartialView("_ExperienceListPartial", new List<ResumeExperienceViewModel>());

            var experience = await _context.ResumeExperiences
                .Where(e => e.JobSeekerProfileId == profile.JobSeekerProfileId)
                .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                .ThenByDescending(e => e.StartDate)
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
                })
                .ToListAsync();

            return PartialView("_ExperienceListPartial", experience);
        }

        /// <summary>
        /// GET: Resume/AddExperience
        /// </summary>
        public async Task<IActionResult> AddExperience()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var viewModel = new ResumeExperienceViewModel
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                StartDate = DateTime.Now.AddYears(-1)
            };

            return PartialView("_ExperienceFormPartial", viewModel);
        }

        /// <summary>
        /// GET: Resume/EditExperience/5
        /// </summary>
        public async Task<IActionResult> EditExperience(int id)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var experience = await _context.ResumeExperiences
                .FirstOrDefaultAsync(e => e.ResumeExperienceId == id && e.JobSeekerProfileId == profile.JobSeekerProfileId);

            if (experience == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Experience record not found." });

            var viewModel = new ResumeExperienceViewModel
            {
                ResumeExperienceId = experience.ResumeExperienceId,
                JobSeekerProfileId = experience.JobSeekerProfileId,
                CompanyName = experience.CompanyName,
                JobTitle = experience.JobTitle,
                StartDate = experience.StartDate,
                EndDate = experience.EndDate,
                Description = experience.Description,
                IsCurrentJob = !experience.EndDate.HasValue
            };

            return PartialView("_ExperienceFormPartial", viewModel);
        }

        /// <summary>
        /// POST: Resume/SaveExperience
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveExperience(ResumeExperienceViewModel model)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new ResumeApiResponse { Success = false, Message = string.Join(", ", errors) });
            }

            try
            {
                if (model.ResumeExperienceId > 0)
                {
                    // Update existing
                    var experience = await _context.ResumeExperiences
                        .FirstOrDefaultAsync(e => e.ResumeExperienceId == model.ResumeExperienceId &&
                                                  e.JobSeekerProfileId == profile.JobSeekerProfileId);

                    if (experience == null)
                        return Json(new ResumeApiResponse { Success = false, Message = "Experience record not found." });

                    experience.CompanyName = model.CompanyName;
                    experience.JobTitle = model.JobTitle;
                    experience.StartDate = model.StartDate;
                    experience.EndDate = model.IsCurrentJob ? null : model.EndDate;
                    experience.Description = model.Description;
                }
                else
                {
                    // Create new
                    var experience = new ResumeExperience
                    {
                        JobSeekerProfileId = profile.JobSeekerProfileId,
                        CompanyName = model.CompanyName,
                        JobTitle = model.JobTitle,
                        StartDate = model.StartDate,
                        EndDate = model.IsCurrentJob ? null : model.EndDate,
                        Description = model.Description
                    };
                    _context.ResumeExperiences.Add(experience);
                }

                // ResumeLastUpdated property removed from model
                await _context.SaveChangesAsync();

                return Json(new ResumeApiResponse { Success = true, Message = "Experience saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving experience");
                return Json(new ResumeApiResponse { Success = false, Message = "An error occurred while saving experience." });
            }
        }

        /// <summary>
        /// POST: Resume/DeleteExperience
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var experience = await _context.ResumeExperiences
                .FirstOrDefaultAsync(e => e.ResumeExperienceId == id && e.JobSeekerProfileId == profile.JobSeekerProfileId);

            if (experience == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Experience record not found." });

            try
            {
                _context.ResumeExperiences.Remove(experience);
                // ResumeLastUpdated property removed from model
                await _context.SaveChangesAsync();

                return Json(new ResumeApiResponse { Success = true, Message = "Experience deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting experience");
                return Json(new ResumeApiResponse { Success = false, Message = "An error occurred while deleting experience." });
            }
        }

        #endregion

        #region Skills CRUD

        /// <summary>
        /// GET: Resume/SkillsList
        /// </summary>
        public async Task<IActionResult> SkillsList()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return PartialView("_SkillsListPartial", new List<ResumeSkillViewModel>());

            var skills = await _context.ResumeSkills
                .Where(s => s.JobSeekerProfileId == profile.JobSeekerProfileId)
                .OrderBy(s => s.SkillName)
                .Select(s => new ResumeSkillViewModel
                {
                    ResumeSkillId = s.ResumeSkillId,
                    JobSeekerProfileId = s.JobSeekerProfileId,
                    SkillName = s.SkillName,
                    SkillLevel = s.SkillLevel
                })
                .ToListAsync();

            return PartialView("_SkillsListPartial", skills);
        }

        /// <summary>
        /// GET: Resume/AddSkill
        /// </summary>
        public async Task<IActionResult> AddSkill()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var viewModel = new ResumeSkillViewModel
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                SkillLevel = "Intermediate",
                SkillLevelOptions = ResumeOptions.GetSkillLevels()
            };

            return PartialView("_SkillFormPartial", viewModel);
        }

        /// <summary>
        /// GET: Resume/EditSkill/5
        /// </summary>
        public async Task<IActionResult> EditSkill(int id)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var skill = await _context.ResumeSkills
                .FirstOrDefaultAsync(s => s.ResumeSkillId == id && s.JobSeekerProfileId == profile.JobSeekerProfileId);

            if (skill == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Skill not found." });

            var viewModel = new ResumeSkillViewModel
            {
                ResumeSkillId = skill.ResumeSkillId,
                JobSeekerProfileId = skill.JobSeekerProfileId,
                SkillName = skill.SkillName,
                SkillLevel = skill.SkillLevel,
                SkillLevelOptions = ResumeOptions.GetSkillLevels()
            };

            return PartialView("_SkillFormPartial", viewModel);
        }

        /// <summary>
        /// POST: Resume/SaveSkill
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSkill(ResumeSkillViewModel model)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new ResumeApiResponse { Success = false, Message = string.Join(", ", errors) });
            }

            try
            {
                if (model.ResumeSkillId > 0)
                {
                    // Update existing
                    var skill = await _context.ResumeSkills
                        .FirstOrDefaultAsync(s => s.ResumeSkillId == model.ResumeSkillId &&
                                                  s.JobSeekerProfileId == profile.JobSeekerProfileId);

                    if (skill == null)
                        return Json(new ResumeApiResponse { Success = false, Message = "Skill not found." });

                    skill.SkillName = model.SkillName;
                    skill.SkillLevel = model.SkillLevel;
                }
                else
                {
                    // Check for duplicate
                    var exists = await _context.ResumeSkills
                        .AnyAsync(s => s.JobSeekerProfileId == profile.JobSeekerProfileId &&
                                       s.SkillName.ToLower() == model.SkillName.ToLower());

                    if (exists)
                        return Json(new ResumeApiResponse { Success = false, Message = "This skill already exists." });

                    // Create new
                    var skill = new ResumeSkill
                    {
                        JobSeekerProfileId = profile.JobSeekerProfileId,
                        SkillName = model.SkillName,
                        SkillLevel = model.SkillLevel
                    };
                    _context.ResumeSkills.Add(skill);
                }

                // ResumeLastUpdated property removed from model
                await _context.SaveChangesAsync();

                return Json(new ResumeApiResponse { Success = true, Message = "Skill saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving skill");
                return Json(new ResumeApiResponse { Success = false, Message = "An error occurred while saving skill." });
            }
        }

        /// <summary>
        /// POST: Resume/DeleteSkill
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            var skill = await _context.ResumeSkills
                .FirstOrDefaultAsync(s => s.ResumeSkillId == id && s.JobSeekerProfileId == profile.JobSeekerProfileId);

            if (skill == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Skill not found." });

            try
            {
                _context.ResumeSkills.Remove(skill);
                // ResumeLastUpdated property removed from model
                await _context.SaveChangesAsync();

                return Json(new ResumeApiResponse { Success = true, Message = "Skill deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting skill");
                return Json(new ResumeApiResponse { Success = false, Message = "An error occurred while deleting skill." });
            }
        }

        /// <summary>
        /// GET: Resume/GetCommonSkills - for autocomplete
        /// </summary>
        [HttpGet]
        public IActionResult GetCommonSkills()
        {
            return Json(ResumeOptions.GetCommonSkills());
        }

        #endregion

        #region Resume Status & Preview

        /// <summary>
        /// POST: Resume/UpdateStatus
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string status)
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new ResumeApiResponse { Success = false, Message = "Profile not found." });

            if (status != "Draft" && status != "Published")
                return Json(new ResumeApiResponse { Success = false, Message = "Invalid status." });

            // Check completion before publishing
            if (status == "Published")
            {
                var completion = await _resumeService.CalculateCompletionPercentageAsync(profile.JobSeekerProfileId);
                if (completion < 50)
                {
                    return Json(new ResumeApiResponse
                    {
                        Success = false,
                        Message = "Please complete at least 50% of your resume before publishing."
                    });
                }
            }

            var success = await _resumeService.UpdateResumeStatusAsync(profile.JobSeekerProfileId, status);

            return Json(new ResumeApiResponse
            {
                Success = success,
                Message = success ? $"Resume {status.ToLower()} successfully." : "Failed to update status."
            });
        }

        /// <summary>
        /// GET: Resume/Preview
        /// </summary>
        public async Task<IActionResult> Preview()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return RedirectToAction("Create", "JobSeekerProfiles");

            var snapshot = await _resumeService.GetResumeSnapshotAsync(profile.JobSeekerProfileId);
            if (snapshot == null)
                return NotFound();

            return View(snapshot);
        }

        /// <summary>
        /// GET: Resume/PreviewPartial
        /// </summary>
        public async Task<IActionResult> PreviewPartial()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return PartialView("_PreviewPartial", new ResumeSnapshotViewModel());

            var snapshot = await _resumeService.GetResumeSnapshotAsync(profile.JobSeekerProfileId);
            return PartialView("_PreviewPartial", snapshot ?? new ResumeSnapshotViewModel());
        }

        /// <summary>
        /// GET: Resume/DownloadPdf
        /// Downloads the current user's resume as a PDF file.
        /// Uses the same template as Preview for visual consistency.
        /// </summary>
        public async Task<IActionResult> DownloadPdf()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
            {
                TempData["ErrorMessage"] = "Please complete your profile first.";
                return RedirectToAction("Create", "JobSeekerProfiles");
            }

            // Check if resume has minimum content
            var hasContent = !string.IsNullOrEmpty(profile.FullName) &&
                (await _context.ResumeEducations.AnyAsync(e => e.JobSeekerProfileId == profile.JobSeekerProfileId) ||
                 await _context.ResumeExperiences.AnyAsync(e => e.JobSeekerProfileId == profile.JobSeekerProfileId) ||
                 await _context.ResumeSkills.AnyAsync(s => s.JobSeekerProfileId == profile.JobSeekerProfileId) ||
                 !string.IsNullOrEmpty(profile.Summary));

            if (!hasContent)
            {
                TempData["ErrorMessage"] = "Your resume is empty. Please add education, experience, or skills before downloading.";
                return RedirectToAction(nameof(Index));
            }

            // Generate PDF using the same HTML template as Preview
            var pdfBytes = await _pdfService.GenerateResumePdfAsync(profile.JobSeekerProfileId);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("Failed to generate PDF for profile {ProfileId}", profile.JobSeekerProfileId);
                TempData["ErrorMessage"] = "PDF generation is currently unavailable. Please use the Print option from your browser instead.";
                return RedirectToAction(nameof(Preview));
            }

            // Generate filename: <FullName>_Resume.pdf
            var safeFileName = profile.FullName?.Replace(" ", "_") ?? "Resume";
            var fileName = $"{safeFileName}_Resume.pdf";

            // Return as downloadable PDF file
            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// GET: Resume/GetCompletionPercentage
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCompletionPercentage()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new { percentage = 0 });

            var percentage = await _resumeService.CalculateCompletionPercentageAsync(profile.JobSeekerProfileId);
            return Json(new { percentage });
        }

        /// <summary>
        /// GET: Resume/GetSectionCounts
        /// Returns the current counts for Education, Experience, and Skills sections
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSectionCounts()
        {
            var profile = await GetCurrentProfileAsync();
            if (profile == null)
                return Json(new { educationCount = 0, experienceCount = 0, skillsCount = 0 });

            var educationCount = await _context.ResumeEducations
                .CountAsync(e => e.JobSeekerProfileId == profile.JobSeekerProfileId);

            var experienceCount = await _context.ResumeExperiences
                .CountAsync(e => e.JobSeekerProfileId == profile.JobSeekerProfileId);

            var skillsCount = await _context.ResumeSkills
                .CountAsync(s => s.JobSeekerProfileId == profile.JobSeekerProfileId);

            return Json(new { educationCount, experienceCount, skillsCount });
        }

        #endregion

        #region Helper Methods

        private async Task<JobSeekerProfile?> GetCurrentProfileAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return null;

            return await _context.JobSeekerProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }

        private async Task<ResumeDashboardViewModel> BuildDashboardViewModelAsync(JobSeekerProfile profile)
        {
            var education = await _context.ResumeEducations
                .Where(e => e.JobSeekerProfileId == profile.JobSeekerProfileId)
                .OrderByDescending(e => e.EndYear ?? 9999)
                .Select(e => new ResumeEducationViewModel
                {
                    ResumeEducationId = e.ResumeEducationId,
                    JobSeekerProfileId = e.JobSeekerProfileId,
                    SchoolName = e.SchoolName,
                    Qualification = e.Qualification,
                    FieldOfStudy = e.FieldOfStudy,
                    StartYear = e.StartYear,
                    EndYear = e.EndYear,
                    IsCurrentlyStudying = !e.EndYear.HasValue,
                    QualificationOptions = ResumeOptions.GetQualifications()
                })
                .ToListAsync();

            var experience = await _context.ResumeExperiences
                .Where(e => e.JobSeekerProfileId == profile.JobSeekerProfileId)
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
                })
                .ToListAsync();

            var skills = await _context.ResumeSkills
                .Where(s => s.JobSeekerProfileId == profile.JobSeekerProfileId)
                .OrderBy(s => s.SkillName)
                .Select(s => new ResumeSkillViewModel
                {
                    ResumeSkillId = s.ResumeSkillId,
                    JobSeekerProfileId = s.JobSeekerProfileId,
                    SkillName = s.SkillName,
                    SkillLevel = s.SkillLevel,
                    SkillLevelOptions = ResumeOptions.GetSkillLevels()
                })
                .ToListAsync();

            return new ResumeDashboardViewModel
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                FullName = profile.FullName,
                Email = profile.User?.Email,
                PhoneNumber = profile.PhoneNumber,
                Summary = profile.Summary,
                ProfilePhoto = profile.ProfilePhoto,
                // ResumeStatus and ResumeLastUpdated properties removed from model
                CompletionPercentage = await _resumeService.CalculateCompletionPercentageAsync(profile.JobSeekerProfileId),
                Education = education,
                Experience = experience,
                Skills = skills
            };
        }

        private async Task<string> SaveUploadedFileAsync(IFormFile file, string folder)
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

