using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Service for managing company reviews.
    /// </summary>
    public class CompanyReviewService : ICompanyReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyReviewService> _logger;

        /// <summary>
        /// Number of days after which an unreported review will be auto-hidden.
        /// </summary>
        private const int AutoHideDays = 3;

        public CompanyReviewService(ApplicationDbContext context, ILogger<CompanyReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsEligibleToReviewAsync(int jobSeekerProfileId, int employerProfileId)
        {
            // A JobSeeker is eligible if they have at least one JobApplication
            // linked to a JobVacancy owned by the target EmployerProfile
            var hasApplication = await _context.JobApplications
                .AnyAsync(ja => 
                    ja.JobSeekerProfileId == jobSeekerProfileId &&
                    ja.JobVacancy.EmployerProfileId == employerProfileId);

            return hasApplication;
        }

        public async Task<bool> HasExistingReviewAsync(int jobSeekerProfileId, int employerProfileId)
        {
            return await _context.CompanyReviews
                .AnyAsync(r => 
                    r.JobSeekerProfileId == jobSeekerProfileId && 
                    r.EmployerProfileId == employerProfileId);
        }

        public async Task<List<CompanyReviewDto>> GetCompanyReviewsAsync(int employerProfileId)
        {
            // Get all visible reviews (Active and UnderReview, not Hidden)
            var reviews = await _context.CompanyReviews
                .Include(r => r.JobSeekerProfile)
                .Where(r => r.EmployerProfileId == employerProfileId && 
                           r.Status != ReviewStatus.Hidden)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new CompanyReviewDto
                {
                    ReviewId = r.ReviewId,
                    EmployerProfileId = r.EmployerProfileId,
                    JobSeekerProfileId = r.JobSeekerProfileId,
                    ReviewerName = r.JobSeekerProfile.FullName,
                    ReviewerPhoto = r.JobSeekerProfile.ProfilePhoto,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ReportedAt = r.ReportedAt,
                    JobseekerResponded = r.JobseekerResponded,
                    ResponseComment = r.ResponseComment,
                    RespondedAt = r.RespondedAt
                })
                .ToListAsync();

            return reviews;
        }

        public async Task<double> GetAverageRatingAsync(int employerProfileId)
        {
            var ratings = await _context.CompanyReviews
                .Where(r => r.EmployerProfileId == employerProfileId && 
                           r.Status != ReviewStatus.Hidden)
                .Select(r => r.Rating)
                .ToListAsync();

            if (!ratings.Any())
                return 0;

            return ratings.Average();
        }

        public async Task<int> GetReviewCountAsync(int employerProfileId)
        {
            return await _context.CompanyReviews
                .CountAsync(r => r.EmployerProfileId == employerProfileId && 
                               r.Status != ReviewStatus.Hidden);
        }

        public async Task<CompanyReview?> SubmitReviewAsync(int jobSeekerProfileId, int employerProfileId, int rating, string comment)
        {
            // Validate eligibility
            if (!await IsEligibleToReviewAsync(jobSeekerProfileId, employerProfileId))
            {
                _logger.LogWarning("JobSeeker {JobSeekerProfileId} is not eligible to review company {EmployerProfileId}",
                    jobSeekerProfileId, employerProfileId);
                return null;
            }

            // Check for existing review
            if (await HasExistingReviewAsync(jobSeekerProfileId, employerProfileId))
            {
                _logger.LogWarning("JobSeeker {JobSeekerProfileId} already has a review for company {EmployerProfileId}",
                    jobSeekerProfileId, employerProfileId);
                return null;
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                _logger.LogWarning("Invalid rating {Rating} provided", rating);
                return null;
            }

            var review = new CompanyReview
            {
                EmployerProfileId = employerProfileId,
                JobSeekerProfileId = jobSeekerProfileId,
                Rating = rating,
                Comment = comment.Trim(),
                Status = ReviewStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.CompanyReviews.Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Review {ReviewId} submitted by JobSeeker {JobSeekerProfileId} for company {EmployerProfileId}",
                review.ReviewId, jobSeekerProfileId, employerProfileId);

            return review;
        }

        public async Task<bool> ReportReviewAsync(int reviewId, int employerUserId)
        {
            var review = await _context.CompanyReviews
                .Include(r => r.EmployerProfile)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
            {
                _logger.LogWarning("Review {ReviewId} not found for reporting", reviewId);
                return false;
            }

            // Only the employer who owns the company can report
            if (review.EmployerProfile.UserId != employerUserId)
            {
                _logger.LogWarning("User {UserId} is not authorized to report review {ReviewId}", 
                    employerUserId, reviewId);
                return false;
            }

            // Can only report once (if already under review or hidden, cannot report again)
            if (review.Status != ReviewStatus.Active)
            {
                _logger.LogWarning("Review {ReviewId} is already reported or hidden", reviewId);
                return false;
            }

            review.Status = ReviewStatus.UnderReview;
            review.ReportedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Review {ReviewId} reported by employer user {UserId}", reviewId, employerUserId);

            return true;
        }

        public async Task<bool> RespondToReportAsync(int reviewId, int jobSeekerProfileId, string responseComment)
        {
            var review = await _context.CompanyReviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
            {
                _logger.LogWarning("Review {ReviewId} not found for response", reviewId);
                return false;
            }

            // Only the original reviewer can respond
            if (review.JobSeekerProfileId != jobSeekerProfileId)
            {
                _logger.LogWarning("JobSeeker {JobSeekerProfileId} is not the author of review {ReviewId}",
                    jobSeekerProfileId, reviewId);
                return false;
            }

            // Can only respond to reviews that are under review
            if (review.Status != ReviewStatus.UnderReview)
            {
                _logger.LogWarning("Review {ReviewId} is not under review, cannot respond", reviewId);
                return false;
            }

            // Can only respond once
            if (review.JobseekerResponded)
            {
                _logger.LogWarning("JobSeeker has already responded to review {ReviewId}", reviewId);
                return false;
            }

            review.JobseekerResponded = true;
            review.ResponseComment = responseComment?.Trim();
            review.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("JobSeeker {JobSeekerProfileId} responded to report on review {ReviewId}",
                jobSeekerProfileId, reviewId);

            return true;
        }

        public async Task<CompanyReview?> GetReviewByIdAsync(int reviewId)
        {
            return await _context.CompanyReviews
                .Include(r => r.EmployerProfile)
                .Include(r => r.JobSeekerProfile)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);
        }

        public async Task<int> ProcessAutoHideReviewsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-AutoHideDays);

            // Find reviews that should be auto-hidden:
            // - Status is UnderReview
            // - JobseekerResponded is false
            // - ReportedAt is older than 3 days
            var reviewsToHide = await _context.CompanyReviews
                .Where(r => 
                    r.Status == ReviewStatus.UnderReview &&
                    !r.JobseekerResponded &&
                    r.ReportedAt != null &&
                    r.ReportedAt <= cutoffDate)
                .ToListAsync();

            if (!reviewsToHide.Any())
            {
                return 0;
            }

            foreach (var review in reviewsToHide)
            {
                review.Status = ReviewStatus.Hidden;
                _logger.LogInformation("Auto-hiding review {ReviewId} - reported at {ReportedAt}, no response after {Days} days",
                    review.ReviewId, review.ReportedAt, AutoHideDays);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Auto-hidden {Count} reviews", reviewsToHide.Count);

            return reviewsToHide.Count;
        }
    }
}



