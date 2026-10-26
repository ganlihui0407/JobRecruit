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
    public class ResumeExperiencesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResumeExperiencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ResumeExperiences/Create (Partial)
        public async Task<IActionResult> Create()
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var viewModel = new ResumeExperienceViewModel
            {
                JobSeekerProfileId = profileId.Value
            };

            return PartialView("_ExperienceForm", viewModel);
        }

        // POST: ResumeExperiences/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResumeExperienceViewModel viewModel)
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            viewModel.JobSeekerProfileId = profileId.Value;

            if (ModelState.IsValid)
            {
                var experience = new ResumeExperience
                {
                    JobSeekerProfileId = viewModel.JobSeekerProfileId,
                    CompanyName = viewModel.CompanyName,
                    JobTitle = viewModel.JobTitle,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.IsCurrentJob ? null : viewModel.EndDate,
                    Description = viewModel.Description
                };

                _context.Add(experience);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Experience added successfully!";
                return RedirectToAction("Edit", "JobSeekerProfiles", new { id = profileId });
            }

            return PartialView("_ExperienceForm", viewModel);
        }

        // GET: ResumeExperiences/Edit/5 (Partial)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var experience = await _context.ResumeExperiences
                .FirstOrDefaultAsync(e => e.ResumeExperienceId == id);

            if (experience == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && experience.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

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

            return PartialView("_ExperienceForm", viewModel);
        }

        // POST: ResumeExperiences/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResumeExperienceViewModel viewModel)
        {
            if (id != viewModel.ResumeExperienceId) return NotFound();

            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var experience = await _context.ResumeExperiences.FindAsync(id);
            if (experience == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && experience.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                experience.CompanyName = viewModel.CompanyName;
                experience.JobTitle = viewModel.JobTitle;
                experience.StartDate = viewModel.StartDate;
                experience.EndDate = viewModel.IsCurrentJob ? null : viewModel.EndDate;
                experience.Description = viewModel.Description;

                _context.Update(experience);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Experience updated successfully!";
                return RedirectToAction("Edit", "JobSeekerProfiles", new { id = profileId });
            }

            return PartialView("_ExperienceForm", viewModel);
        }

        // POST: ResumeExperiences/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var experience = await _context.ResumeExperiences.FindAsync(id);
            if (experience == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && experience.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            _context.ResumeExperiences.Remove(experience);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Experience removed successfully!";
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

