using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Service interface for calculating resume completion percentage.
    /// This provides a single source of truth for completion calculation
    /// used by both the Jobseeker Dashboard and Resume Builder.
    /// </summary>
    public interface IResumeCompletionService
    {
        /// <summary>
        /// Calculate resume completion percentage from a JobSeekerProfile entity.
        /// Profile must include ResumeEducations, ResumeExperiences, and ResumeSkills collections.
        /// </summary>
        /// <param name="profile">The JobSeekerProfile with related resume data loaded</param>
        /// <returns>Completion percentage from 0 to 100</returns>
        int CalculateCompletion(JobSeekerProfile profile);

        /// <summary>
        /// Calculate resume completion percentage by fetching profile from database.
        /// </summary>
        /// <param name="jobSeekerProfileId">The profile ID to calculate completion for</param>
        /// <returns>Completion percentage from 0 to 100</returns>
        Task<int> CalculateCompletionAsync(int jobSeekerProfileId);
    }
}
































