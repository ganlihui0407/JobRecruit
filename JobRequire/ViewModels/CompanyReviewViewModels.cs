using System.ComponentModel.DataAnnotations;
using JobRequire.Services;

namespace JobRequire.ViewModels
{
    // =============================================
    // COMPANY REVIEW VIEW MODELS
    // =============================================
    // Purpose: ViewModels for company review feature.
    // Includes: Review list, Create review, Respond to report.
    // 
    // Design Decision: Reviews are tied to EmployerProfile (company).
    // Only JobSeekers who have applied can submit reviews.
    // Employers can report reviews; JobSeekers can respond.
    // =============================================

    /// <summary>
    /// ViewModel for displaying the company reviews page.
    /// Shows all reviews with eligibility info for current user.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Companies/{id}/Reviews
    /// </remarks>
    public class CompanyReviewsViewModel
    {
        public int EmployerProfileId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        
        public List<CompanyReviewDto> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        
        /// <summary>
        /// Whether the current user can submit a review (is eligible and hasn't already reviewed).
        /// </summary>
        public bool CanSubmitReview { get; set; }
        
        /// <summary>
        /// Whether the current user has already submitted a review.
        /// </summary>
        public bool HasExistingReview { get; set; }
        
        /// <summary>
        /// Whether the current user is eligible to review (has applied to this company).
        /// </summary>
        public bool IsEligibleToReview { get; set; }
        
        /// <summary>
        /// Whether the current user is the employer who owns this company.
        /// </summary>
        public bool IsCompanyOwner { get; set; }
        
        /// <summary>
        /// Current user's JobSeekerProfileId (if they are a JobSeeker).
        /// </summary>
        public int? CurrentJobSeekerProfileId { get; set; }

        /// <summary>
        /// Display-friendly average rating (e.g., "4.5").
        /// </summary>
        public string AverageRatingDisplay => AverageRating > 0 ? AverageRating.ToString("F1") : "N/A";

        /// <summary>
        /// Gets the rating distribution for display.
        /// </summary>
        public Dictionary<int, int> GetRatingDistribution()
        {
            var distribution = new Dictionary<int, int>
            {
                { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 }
            };

            foreach (var review in Reviews)
            {
                if (distribution.ContainsKey(review.Rating))
                {
                    distribution[review.Rating]++;
                }
            }

            return distribution;
        }
    }

    /// <summary>
    /// ViewModel for the create review form.
    /// JobSeeker submits rating and comment for a company.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Companies/{id}/Reviews/Create
    /// </remarks>
    public class CreateReviewViewModel
    {
        public int EmployerProfileId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please enter a comment.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 2000 characters.")]
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for the respond to review report form.
    /// JobSeeker responds to an employer's report on their review.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Companies/Reviews/{id}/Respond
    /// If JobSeeker responds, review won't be auto-hidden after 3 days.
    /// </remarks>
    public class RespondToReportViewModel
    {
        public int ReviewId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string OriginalComment { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime? ReportedAt { get; set; }

        [Required(ErrorMessage = "Please enter a response.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Response must be between 10 and 1000 characters.")]
        public string ResponseComment { get; set; } = string.Empty;

        /// <summary>
        /// Days remaining before auto-hide (if no response).
        /// </summary>
        public int DaysRemaining
        {
            get
            {
                if (!ReportedAt.HasValue) return 3;
                var elapsed = DateTime.UtcNow - ReportedAt.Value;
                var remaining = 3 - (int)elapsed.TotalDays;
                return Math.Max(0, remaining);
            }
        }
    }
}



