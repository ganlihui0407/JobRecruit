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
    public class ResumeSkillsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResumeSkillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ResumeSkills/Create (Partial)
        public async Task<IActionResult> Create()
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var viewModel = new ResumeSkillViewModel
            {
                JobSeekerProfileId = profileId.Value,
                SkillLevelOptions = ResumeOptions.GetSkillLevels()
            };

            return PartialView("_SkillForm", viewModel);
        }

        // POST: ResumeSkills/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResumeSkillViewModel viewModel)
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            viewModel.JobSeekerProfileId = profileId.Value;

            if (ModelState.IsValid)
            {
                var skill = new ResumeSkill
                {
                    JobSeekerProfileId = viewModel.JobSeekerProfileId,
                    SkillName = viewModel.SkillName,
                    SkillLevel = viewModel.SkillLevel
                };

                _context.Add(skill);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Skill added successfully!";
                return RedirectToAction("Edit", "JobSeekerProfiles", new { id = profileId });
            }

            viewModel.SkillLevelOptions = ResumeOptions.GetSkillLevels();
            return PartialView("_SkillForm", viewModel);
        }

        // GET: ResumeSkills/Edit/5 (Partial)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var skill = await _context.ResumeSkills
                .FirstOrDefaultAsync(s => s.ResumeSkillId == id);

            if (skill == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && skill.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            var viewModel = new ResumeSkillViewModel
            {
                ResumeSkillId = skill.ResumeSkillId,
                JobSeekerProfileId = skill.JobSeekerProfileId,
                SkillName = skill.SkillName,
                SkillLevel = skill.SkillLevel,
                SkillLevelOptions = ResumeOptions.GetSkillLevels()
            };

            return PartialView("_SkillForm", viewModel);
        }

        // POST: ResumeSkills/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResumeSkillViewModel viewModel)
        {
            if (id != viewModel.ResumeSkillId) return NotFound();

            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var skill = await _context.ResumeSkills.FindAsync(id);
            if (skill == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && skill.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                skill.SkillName = viewModel.SkillName;
                skill.SkillLevel = viewModel.SkillLevel;

                _context.Update(skill);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Skill updated successfully!";
                return RedirectToAction("Edit", "JobSeekerProfiles", new { id = profileId });
            }

            viewModel.SkillLevelOptions = ResumeOptions.GetSkillLevels();
            return PartialView("_SkillForm", viewModel);
        }

        // POST: ResumeSkills/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var profileId = await GetCurrentProfileId();
            if (profileId == null) return Unauthorized();

            var skill = await _context.ResumeSkills.FindAsync(id);
            if (skill == null) return NotFound();

            // Check authorization
            if (!User.IsInRole("Admin") && skill.JobSeekerProfileId != profileId)
            {
                return Forbid();
            }

            _context.ResumeSkills.Remove(skill);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Skill removed successfully!";
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

