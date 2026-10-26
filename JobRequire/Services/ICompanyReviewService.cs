using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Service interface for managing company reviews.
    /// </summary>
    public interface ICompanyReviewService
    {
        /// <summary>
        /// Checks if a JobSeeker is eligible to submit a review for a company.
        /// A JobSeeker is eligible if they have at least one JobApplication 
        /// linked to a JobVacancy owned by the target EmployerProfile.
        /// </summary>
        Task<bool> IsEligibleToReviewAsync(int jobSeekerProfileId, int employerProfileId);

        /// <summary>
        /// Checks if a JobSeeker has already submitted a review for a company.
        /// </summary>
        Task<bool> HasExistingReviewAsync(int jobSeekerProfileId, int employerProfileId);

        /// <summary>
        /// Gets all visible reviews for a company (Active and UnderReview status).
        /// Hidden reviews are not included.
        /// </summary>
        Task<List<CompanyReviewDto>> GetCompanyReviewsAsync(int employerProfileId);

        /// <summary>
        /// Gets the average rating for a company.
        /// </summary>
        Task<double> GetAverageRatingAsync(int employerProfileId);

        /// <summary>
        /// Gets the total count of visible reviews for a company.
        /// </summary>
        Task<int> GetReviewCountAsync(int employerProfileId);

        /// <summary>
        /// Submits a new review for a company.
        /// </summary>
        Task<CompanyReview?> SubmitReviewAsync(int jobSeekerProfileId, int employerProfileId, int rating, string comment);

        /// <summary>
        /// Reports a review (employer action).
        /// Only the employer who owns the company can report reviews.
        /// </summary>
        Task<bool> ReportReviewAsync(int reviewId, int employerUserId);

        /// <summary>
        /// Responds to a review report (jobseeker action).
        /// Only the original reviewer can respond.
        /// </summary>
        Task<bool> RespondToReportAsync(int reviewId, int jobSeekerProfileId, string responseComment);

        /// <summary>
        /// Gets a review by ID.
        /// </summary>
        Task<CompanyReview?> GetReviewByIdAsync(int reviewId);

        /// <summary>
        /// Processes auto-hide logic for reviews that have been under review for 3+ days
        /// without a JobSeeker response.
        /// </summary>
        Task<int> ProcessAutoHideReviewsAsync();
    }

    /// <summary>
    /// DTO for displaying company reviews.
    /// </summary>
    public class CompanyReviewDto
    {
        public int ReviewId { get; set; }
        public int EmployerProfileId { get; set; }
        public int JobSeekerProfileId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string? ReviewerPhoto { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReportedAt { get; set; }
        public bool JobseekerResponded { get; set; }
        public string? ResponseComment { get; set; }
        public DateTime? RespondedAt { get; set; }
        
        /// <summary>
        /// Whether the review is under review (reported by employer).
        /// </summary>
        public bool IsUnderReview => Status == ReviewStatus.UnderReview;
        
        /// <summary>
        /// Display-friendly time since the review was created.
        /// </summary>
        public string TimeAgo => GetTimeAgo(CreatedAt);

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;

            if (span.TotalDays >= 365)
                return $"{(int)(span.TotalDays / 365)}y ago";
            if (span.TotalDays >= 30)
                return $"{(int)(span.TotalDays / 30)}mo ago";
            if (span.TotalDays >= 7)
                return $"{(int)(span.TotalDays / 7)}w ago";
            if (span.TotalDays >= 1)
                return $"{(int)span.TotalDays}d ago";
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h ago";
            if (span.TotalMinutes >= 1)
                return $"{(int)span.TotalMinutes}m ago";
            return "Just now";
        }
    }
}



