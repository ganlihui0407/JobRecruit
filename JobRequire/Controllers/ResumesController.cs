using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Services;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for viewing job seeker resumes (read-only for employers)
    /// </summary>
    [Authorize(Roles = "Employer,Admin,JobSeeker")]
    public class ResumesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;
        private readonly ILogger<ResumesController> _logger;

        public ResumesController(
            ApplicationDbContext context, 
            IPdfService pdfService,
            ILogger<ResumesController> logger)
        {
            _context = context;
            _pdfService = pdfService;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }

        /// <summary>
        /// GET: Resumes/View/{jobSeekerId}
        /// Displays a read-only view of a job seeker's website resume
        /// </summary>
        /// <param name="jobSeekerId">The JobSeekerProfileId of the job seeker</param>
        /// <returns>Read-only resume view</returns>
        [HttpGet]
        [Authorize(Roles = "Employer,Admin")]
        public async Task<IActionResult> View(int jobSeekerId)
        {
            // Load the job seeker profile with all resume data
            var profile = await _context.JobSeekerProfiles
                .Include(p => p.User)
                .Include(p => p.ResumeEducations)
                .Include(p => p.ResumeExperiences)
                .Include(p => p.ResumeSkills)
                .FirstOrDefaultAsync(p => p.JobSeekerProfileId == jobSeekerId);

            if (profile == null)
            {
                _logger.LogWarning("Resume not found for job seeker profile {JobSeekerProfileId}", jobSeekerId);
                return NotFound("Resume not found.");
            }

            // Build the view model
            var viewModel = new ResumeViewViewModel
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                UserId = profile.UserId,
                FullName = profile.FullName,
                Email = profile.User?.Email,
                PhoneNumber = profile.PhoneNumber,
                Summary = profile.Summary,
                ProfilePhoto = profile.ProfilePhoto,
                Education = profile.ResumeEducations
                    .OrderByDescending(e => e.EndYear ?? 9999)
                    .ThenByDescending(e => e.StartYear)
                    .Select(e => new ResumeEducationViewModel
                    {
                        ResumeEducationId = e.ResumeEducationId,
                        SchoolName = e.SchoolName,
                        Qualification = e.Qualification,
                        FieldOfStudy = e.FieldOfStudy,
                        StartYear = e.StartYear,
                        EndYear = e.EndYear
                    }).ToList(),
                Experience = profile.ResumeExperiences
                    .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                    .ThenByDescending(e => e.StartDate)
                    .Select(e => new ResumeExperienceViewModel
                    {
                        ResumeExperienceId = e.ResumeExperienceId,
                        CompanyName = e.CompanyName,
                        JobTitle = e.JobTitle,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Description = e.Description
                    }).ToList(),
                Skills = profile.ResumeSkills
                    .OrderBy(s => s.SkillName)
                    .Select(s => new ResumeSkillViewModel
                    {
                        ResumeSkillId = s.ResumeSkillId,
                        SkillName = s.SkillName,
                        SkillLevel = s.SkillLevel ?? "Intermediate"
                    }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// GET: Resumes/DownloadPdf/{jobSeekerId}
        /// Downloads resume as PDF for employers viewing applicant resumes
        /// </summary>
        /// <param name="jobSeekerId">The JobSeekerProfileId of the job seeker</param>
        /// <returns>PDF file download</returns>
        [HttpGet]
        [Authorize(Roles = "Employer,Admin")]
        public async Task<IActionResult> DownloadPdf(int jobSeekerId)
        {
            var profile = await _context.JobSeekerProfiles
                .Include(p => p.ResumeEducations)
                .Include(p => p.ResumeExperiences)
                .Include(p => p.ResumeSkills)
                .FirstOrDefaultAsync(p => p.JobSeekerProfileId == jobSeekerId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Resume not found.";
                return RedirectToAction("Index", "JobApplications");
            }

            // Check if resume has minimum content
            bool hasContent = !string.IsNullOrEmpty(profile.FullName) &&
                (profile.ResumeEducations.Any() || 
                 profile.ResumeExperiences.Any() || 
                 profile.ResumeSkills.Any() ||
                 !string.IsNullOrEmpty(profile.Summary));

            if (!hasContent)
            {
                TempData["ErrorMessage"] = "This applicant's resume is empty.";
                return RedirectToAction("View", new { jobSeekerId });
            }

            var pdfBytes = await _pdfService.GenerateResumePdfAsync(jobSeekerId);
            
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("Failed to generate PDF for job seeker profile {JobSeekerProfileId}", jobSeekerId);
                TempData["ErrorMessage"] = "PDF generation is currently unavailable. Please view the resume online instead.";
                return RedirectToAction("View", new { jobSeekerId });
            }

            var fileName = $"{profile.FullName.Replace(" ", "_")}_Resume.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// GET: Resumes/MyResumePdf
        /// Downloads the current job seeker's own resume as PDF
        /// </summary>
        /// <returns>PDF file download</returns>
        [HttpGet]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> MyResumePdf()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var profile = await _context.JobSeekerProfiles
                .Include(p => p.ResumeEducations)
                .Include(p => p.ResumeExperiences)
                .Include(p => p.ResumeSkills)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Please complete your profile first.";
                return RedirectToAction("Create", "JobSeekerProfiles");
            }

            // Check if resume has minimum content
            bool hasContent = !string.IsNullOrEmpty(profile.FullName) &&
                (profile.ResumeEducations.Any() || 
                 profile.ResumeExperiences.Any() || 
                 profile.ResumeSkills.Any() ||
                 !string.IsNullOrEmpty(profile.Summary));

            if (!hasContent)
            {
                TempData["ErrorMessage"] = "Your resume is empty. Please add education, experience, or skills before downloading.";
                return RedirectToAction("Index", "Resume");
            }

            var pdfBytes = await _pdfService.GenerateResumePdfAsync(profile.JobSeekerProfileId);
            
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("Failed to generate PDF for user {UserId}", userId);
                TempData["ErrorMessage"] = "PDF generation is currently unavailable. Please use the Print option from your browser instead.";
                return RedirectToAction("Preview", "Resume");
            }

            var fileName = $"{profile.FullName.Replace(" ", "_")}_Resume.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}

