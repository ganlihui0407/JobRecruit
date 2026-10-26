using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Services;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for managing job interests (popularity tracking).
    /// Only JobSeekers can toggle interest, but everyone can see counts.
    /// </summary>
    public class JobInterestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJobInterestService _interestService;
        private readonly ILogger<JobInterestsController> _logger;

        public JobInterestsController(
            ApplicationDbContext context,
            IJobInterestService interestService,
            ILogger<JobInterestsController> logger)
        {
            _context = context;
            _interestService = interestService;
            _logger = logger;
        }

        /// <summary>
        /// POST: /api/jobs/interest - Toggle interest for a job
        /// Only accessible by JobSeekers.
        /// </summary>
        [HttpPost]
        [Route("api/jobs/interest")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> ToggleInterest([FromBody] InterestRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new InterestApiResponse
                    {
                        Success = false,
                        Message = "Please log in to mark interest."
                    });
                }

                // Get JobSeeker profile
                var profile = await _context.JobSeekerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    return Json(new InterestApiResponse
                    {
                        Success = false,
                        Message = "Job seeker profile not found."
                    });
                }

                // Check if job exists
                var jobExists = await _context.JobVacancies
                    .AnyAsync(j => j.JobVacancyId == request.JobVacancyId);

                if (!jobExists)
                {
                    return Json(new InterestApiResponse
                    {
                        Success = false,
                        Message = "Job not found."
                    });
                }

                // Toggle interest
                var (isInterested, interestedCount) = await _interestService
                    .ToggleInterestAsync(request.JobVacancyId, profile.JobSeekerProfileId);

                return Json(new InterestApiResponse
                {
                    Success = true,
                    IsInterested = isInterested,
                    InterestedCount = interestedCount,
                    Message = isInterested 
                        ? "You've marked interest in this job!" 
                        : "Interest removed."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling interest for job {JobId}", request.JobVacancyId);
                return Json(new InterestApiResponse
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                });
            }
        }

        /// <summary>
        /// GET: /api/jobs/interest/{jobVacancyId} - Get interest count for a job
        /// Accessible by everyone (no auth required).
        /// </summary>
        [HttpGet]
        [Route("api/jobs/interest/{jobVacancyId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInterestCount(int jobVacancyId)
        {
            try
            {
                var count = await _interestService.GetInterestedCountAsync(jobVacancyId);
                
                // Check if current user is interested (if logged in as JobSeeker)
                bool isInterested = false;
                var userId = GetCurrentUserId();
                if (userId.HasValue && User.IsInRole("JobSeeker"))
                {
                    var profile = await _context.JobSeekerProfiles
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    if (profile != null)
                    {
                        isInterested = await _interestService
                            .IsInterestedAsync(jobVacancyId, profile.JobSeekerProfileId);
                    }
                }

                return Json(new InterestApiResponse
                {
                    Success = true,
                    InterestedCount = count,
                    IsInterested = isInterested
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interest count for job {JobId}", jobVacancyId);
                return Json(new InterestApiResponse
                {
                    Success = false,
                    Message = "An error occurred."
                });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
    }

    /// <summary>
    /// Request model for interest API
    /// </summary>
    public class InterestRequest
    {
        public int JobVacancyId { get; set; }
    }

    /// <summary>
    /// Response model for interest API
    /// </summary>
    public class InterestApiResponse
    {
        public bool Success { get; set; }
        public bool IsInterested { get; set; }
        public int InterestedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}




















