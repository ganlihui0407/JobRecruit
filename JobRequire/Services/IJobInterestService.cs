namespace JobRequire.Services
{
    /// <summary>
    /// Service interface for managing job interests (popularity tracking).
    /// </summary>
    public interface IJobInterestService
    {
        /// <summary>
        /// Check if a job seeker has marked interest in a specific job.
        /// </summary>
        Task<bool> IsInterestedAsync(int jobVacancyId, int jobSeekerProfileId);

        /// <summary>
        /// Toggle interest for a job (add if not exists, remove if exists).
        /// </summary>
        /// <returns>Tuple of (isInterested after toggle, total interested count)</returns>
        Task<(bool IsInterested, int InterestedCount)> ToggleInterestAsync(int jobVacancyId, int jobSeekerProfileId);

        /// <summary>
        /// Get the total count of job seekers interested in a specific job.
        /// </summary>
        Task<int> GetInterestedCountAsync(int jobVacancyId);

        /// <summary>
        /// Get interested counts for multiple jobs at once (for list views).
        /// </summary>
        Task<Dictionary<int, int>> GetInterestedCountsAsync(IEnumerable<int> jobVacancyIds);

        /// <summary>
        /// Check if a job seeker has marked interest in multiple jobs (for list views).
        /// </summary>
        Task<Dictionary<int, bool>> GetUserInterestsAsync(IEnumerable<int> jobVacancyIds, int jobSeekerProfileId);
    }
}




















