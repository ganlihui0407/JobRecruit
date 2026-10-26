using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Service for managing job interests (popularity tracking).
    /// </summary>
    public class JobInterestService : IJobInterestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobInterestService> _logger;

        public JobInterestService(ApplicationDbContext context, ILogger<JobInterestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> IsInterestedAsync(int jobVacancyId, int jobSeekerProfileId)
        {
            return await _context.JobInterests
                .AnyAsync(ji => ji.JobVacancyId == jobVacancyId && 
                               ji.JobSeekerProfileId == jobSeekerProfileId);
        }

        /// <inheritdoc/>
        public async Task<(bool IsInterested, int InterestedCount)> ToggleInterestAsync(int jobVacancyId, int jobSeekerProfileId)
        {
            var existingInterest = await _context.JobInterests
                .FirstOrDefaultAsync(ji => ji.JobVacancyId == jobVacancyId && 
                                          ji.JobSeekerProfileId == jobSeekerProfileId);

            bool isInterested;

            if (existingInterest != null)
            {
                // Remove interest
                _context.JobInterests.Remove(existingInterest);
                await _context.SaveChangesAsync();
                isInterested = false;
                _logger.LogInformation("JobSeeker {ProfileId} removed interest from Job {JobId}", 
                    jobSeekerProfileId, jobVacancyId);
            }
            else
            {
                // Add interest
                var newInterest = new JobInterest
                {
                    JobVacancyId = jobVacancyId,
                    JobSeekerProfileId = jobSeekerProfileId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.JobInterests.Add(newInterest);
                await _context.SaveChangesAsync();
                isInterested = true;
                _logger.LogInformation("JobSeeker {ProfileId} marked interest in Job {JobId}", 
                    jobSeekerProfileId, jobVacancyId);
            }

            // Get updated count
            var count = await GetInterestedCountAsync(jobVacancyId);
            return (isInterested, count);
        }

        /// <inheritdoc/>
        public async Task<int> GetInterestedCountAsync(int jobVacancyId)
        {
            return await _context.JobInterests
                .CountAsync(ji => ji.JobVacancyId == jobVacancyId);
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, int>> GetInterestedCountsAsync(IEnumerable<int> jobVacancyIds)
        {
            var jobIds = jobVacancyIds.ToList();
            
            var counts = await _context.JobInterests
                .Where(ji => jobIds.Contains(ji.JobVacancyId))
                .GroupBy(ji => ji.JobVacancyId)
                .Select(g => new { JobVacancyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.JobVacancyId, x => x.Count);

            // Ensure all requested job IDs have an entry (0 if no interests)
            foreach (var jobId in jobIds)
            {
                if (!counts.ContainsKey(jobId))
                {
                    counts[jobId] = 0;
                }
            }

            return counts;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, bool>> GetUserInterestsAsync(IEnumerable<int> jobVacancyIds, int jobSeekerProfileId)
        {
            var jobIds = jobVacancyIds.ToList();
            
            var interestedJobIds = await _context.JobInterests
                .Where(ji => jobIds.Contains(ji.JobVacancyId) && 
                            ji.JobSeekerProfileId == jobSeekerProfileId)
                .Select(ji => ji.JobVacancyId)
                .ToListAsync();

            var result = new Dictionary<int, bool>();
            foreach (var jobId in jobIds)
            {
                result[jobId] = interestedJobIds.Contains(jobId);
            }

            return result;
        }
    }
}




















