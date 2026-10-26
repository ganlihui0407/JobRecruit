using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    [Authorize(Roles = "JobSeeker,Admin")]
    public class ResumeEducationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResumeEducationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ResumeEducations/Create (Partial)
        public async Task<IActionResult> Create()
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var viewModel = new ResumeEducationViewModel
            {
                JobSeekerProfileId = profileId.Value,
                QualificationOptions = ResumeOptions.GetQualifications()
            };

            return PartialView("_EducationForm", viewModel);
        }

        // POST: ResumeEducations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResumeEducationViewModel viewModel)
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            viewModel.JobSeekerProfileId = profileId.Value;

            if (ModelState.IsValid)
            {
                var education = new ResumeEducation
                {
                    JobSeekerProfileId = viewModel.JobSeekerProfileId,
                    SchoolName = viewModel.SchoolName,
                    Qualification = viewModel.Qualification,
                    FieldOfStudy = viewModel.FieldOfStudy,
                    StartYear = viewModel.StartYear,
                    EndYear = viewModel.EndYear
                };

                _context.Add(education);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Education added successfully!";
                return RedirectToAction("Edit", "JobSeekerProfiles", new { id = profileId });
            }

            viewModel.QualificationOptions = ResumeOptions.GetQualifications();
            return PartialView("_EducationForm", viewModel);
        }

        // GET: ResumeEducations/Edit/5 (Partial)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var education = await _context.ResumeEducations
                .FirstOrDefaultAsync(e => e.ResumeEducationId == id);

            if (education == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && education.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            var viewModel = new ResumeEducationViewModel
            {
                ResumeEducationId = education.ResumeEducationId,
                JobSeekerProfileId = education.JobSeekerProfileId,
                SchoolName = education.SchoolName,
                Qualification = education.Qualification,
                FieldOfStudy = education.FieldOfStudy,
                StartYear = education.StartYear,
                EndYear = education.EndYear,
                QualificationOptions = ResumeOptions.GetQualifications()
            };

            return PartialView("_EducationForm", viewModel);
        }

        // POST: ResumeEducations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResumeEducationViewModel viewModel)
        {
            if (id != viewModel.ResumeEducationId) return NotFound();

            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var education = await _context.ResumeEducations.FindAsync(id);
            if (education == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && education.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                education.SchoolName = viewModel.SchoolName;
                education.Qualification = viewModel.Qualification;
                education.FieldOfStudy = viewModel.FieldOfStudy;
                education.StartYear = viewModel.StartYear;
                education.EndYear = viewModel.EndYear;

                _context.Update(education);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Education updated successfully!";
                return RedirectToAction("Edit", "JobSeekerProfiles", new { id = profileId });
            }

            viewModel.QualificationOptions = ResumeOptions.GetQualifications();
            return PartialView("_EducationForm", viewModel);
        }

        // POST: ResumeEducations/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var education = await _context.ResumeEducations.FindAsync(id);
            if (education == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && education.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            _context.ResumeEducations.Remove(education);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Education removed successfully!";
            return RedirectToAction("Edit", "JobSeekerProfiles", new { id = profileId });
        }

        private async Task<int?> GetCurrentProfileId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId)) return null;

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == userId);

            return profile?.JobSeekerProfileId;
        }
    }
}

