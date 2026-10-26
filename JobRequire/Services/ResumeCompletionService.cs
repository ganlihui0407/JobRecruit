using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Centralized service for calculating resume completion percentage.
    /// This is the SINGLE SOURCE OF TRUTH for completion calculation,
    /// used by both the Jobseeker Dashboard and Resume Builder.
    /// 
    /// Weighting breakdown (total 100%):
    /// - Profile Info (30%): FullName 15%, Summary 15%
    /// - Education (25%): At least 1 education entry
    /// - Experience (25%): At least 1 experience entry
    /// - Skills (20%): At least 1 skill entry
    /// </summary>
    public class ResumeCompletionService : IResumeCompletionService
    {
        private readonly ApplicationDbContext _context;

        // Completion weights - these match the original JobApplicationsController formula
        private const int WeightFullName = 15;
        private const int WeightSummary = 15;
        private const int WeightEducation = 25;
        private const int WeightExperience = 25;
        private const int WeightSkills = 20;

        public ResumeCompletionService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculate resume completion percentage from a JobSeekerProfile entity.
        /// Profile must include ResumeEducations, ResumeExperiences, and ResumeSkills collections.
        /// </summary>
        /// <param name="profile">The JobSeekerProfile with related resume data loaded</param>
        /// <returns>Completion percentage from 0 to 100</returns>
        public int CalculateCompletion(JobSeekerProfile profile)
        {
            if (profile == null)
                return 0;

            int score = 0;

            // Profile basics (30%)
            if (!string.IsNullOrWhiteSpace(profile.FullName))
                score += WeightFullName;

            if (!string.IsNullOrWhiteSpace(profile.Summary))
                score += WeightSummary;

            // Education (25%)
            var educations = profile.ResumeEducations;
            if (educations != null && educations.Any())
                score += WeightEducation;

            // Experience (25%)
            var experiences = profile.ResumeExperiences;
            if (experiences != null && experiences.Any())
                score += WeightExperience;

            // Skills (20%)
            var skills = profile.ResumeSkills;
            if (skills != null && skills.Any())
                score += WeightSkills;

            return Math.Min(score, 100);
        }

        /// <summary>
        /// Calculate resume completion percentage by fetching profile from database.
        /// </summary>
        /// <param name="jobSeekerProfileId">The profile ID to calculate completion for</param>
        /// <returns>Completion percentage from 0 to 100</returns>
        public async Task<int> CalculateCompletionAsync(int jobSeekerProfileId)
        {
            var profile = await _context.JobSeekerProfiles
                .Include(p => p.ResumeEducations)
                .Include(p => p.ResumeExperiences)
                .Include(p => p.ResumeSkills)
                .FirstOrDefaultAsync(p => p.JobSeekerProfileId == jobSeekerProfileId);

            if (profile == null)
                return 0;

            return CalculateCompletion(profile);
        }
    }
}

