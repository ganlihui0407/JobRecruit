using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.Services;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for managing company reviews.
    /// Reviews are visible to everyone but can only be submitted by eligible JobSeekers.
    /// </summary>
    public class CompanyReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompanyReviewService _reviewService;
        private readonly ILogger<CompanyReviewsController> _logger;

        public CompanyReviewsController(
            ApplicationDbContext context,
            ICompanyReviewService reviewService,
            ILogger<CompanyReviewsController> logger)
        {
            _context = context;
            _reviewService = reviewService;
            _logger = logger;
        }

        /// <summary>
        /// GET: CompanyReviews/Index/{employerProfileId}
        /// Shows all reviews for a company. Accessible to everyone.
        /// </summary>
        [HttpGet]
        [Route("Companies/{employerProfileId}/Reviews")]
        public async Task<IActionResult> Index(int employerProfileId)
        {
            var company = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.EmployerProfileId == employerProfileId);

            if (company == null)
            {
                return NotFound();
            }

            var reviews = await _reviewService.GetCompanyReviewsAsync(employerProfileId);
            var averageRating = await _reviewService.GetAverageRatingAsync(employerProfileId);
            var reviewCount = await _reviewService.GetReviewCountAsync(employerProfileId);

            // Check if current user can submit a review
            var canSubmitReview = false;
            var hasExistingReview = false;
            var isEligible = false;
            int? currentJobSeekerProfileId = null;
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole == "JobSeeker" && currentUserId.HasValue)
            {
                var jobSeekerProfile = await _context.JobSeekerProfiles
                    .FirstOrDefaultAsync(j => j.UserId == currentUserId.Value);

                if (jobSeekerProfile != null)
                {
                    currentJobSeekerProfileId = jobSeekerProfile.JobSeekerProfileId;
                    isEligible = await _reviewService.IsEligibleToReviewAsync(jobSeekerProfile.JobSeekerProfileId, employerProfileId);
                    hasExistingReview = await _reviewService.HasExistingReviewAsync(jobSeekerProfile.JobSeekerProfileId, employerProfileId);
                    canSubmitReview = isEligible && !hasExistingReview;
                }
            }

            // Check if current user is the employer who owns this company
            var isCompanyOwner = false;
            if (currentUserRole == "Employer" && currentUserId.HasValue)
            {
                isCompanyOwner = company.UserId == currentUserId.Value;
            }

            var viewModel = new CompanyReviewsViewModel
            {
                EmployerProfileId = employerProfileId,
                CompanyName = company.CompanyName,
                CompanyLogo = company.CompanyLogo,
                Reviews = reviews,
                AverageRating = averageRating,
                ReviewCount = reviewCount,
                CanSubmitReview = canSubmitReview,
                HasExistingReview = hasExistingReview,
                IsEligibleToReview = isEligible,
                IsCompanyOwner = isCompanyOwner,
                CurrentJobSeekerProfileId = currentJobSeekerProfileId
            };

            return View(viewModel);
        }

        /// <summary>
        /// GET: CompanyReviews/Create/{employerProfileId}
        /// Shows the review submission form. Only for eligible JobSeekers.
        /// </summary>
        [Authorize(Roles = "JobSeeker")]
        [HttpGet]
        [Route("Companies/{employerProfileId}/Reviews/Create")]
        public async Task<IActionResult> Create(int employerProfileId)
        {
            var company = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.EmployerProfileId == employerProfileId);

            if (company == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var jobSeekerProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == currentUserId.Value);

            if (jobSeekerProfile == null)
            {
                TempData["ErrorMessage"] = "JobSeeker profile not found.";
                return RedirectToAction("Index", new { employerProfileId });
            }

            // Check eligibility
            if (!await _reviewService.IsEligibleToReviewAsync(jobSeekerProfile.JobSeekerProfileId, employerProfileId))
            {
                TempData["ErrorMessage"] = "You are not eligible to review this company. You must have applied to at least one job at this company.";
                return RedirectToAction("Index", new { employerProfileId });
            }

            // Check for existing review
            if (await _reviewService.HasExistingReviewAsync(jobSeekerProfile.JobSeekerProfileId, employerProfileId))
            {
                TempData["ErrorMessage"] = "You have already submitted a review for this company.";
                return RedirectToAction("Index", new { employerProfileId });
            }

            var viewModel = new CreateReviewViewModel
            {
                EmployerProfileId = employerProfileId,
                CompanyName = company.CompanyName
            };

            return View(viewModel);
        }

        /// <summary>
        /// POST: CompanyReviews/Create
        /// Submits a new review. Only for eligible JobSeekers.
        /// </summary>
        [Authorize(Roles = "JobSeeker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Companies/{employerProfileId}/Reviews/Create")]
        public async Task<IActionResult> Create(int employerProfileId, CreateReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var jobSeekerProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == currentUserId.Value);

            if (jobSeekerProfile == null)
            {
                TempData["ErrorMessage"] = "JobSeeker profile not found.";
                return RedirectToAction("Index", new { employerProfileId });
            }

            var review = await _reviewService.SubmitReviewAsync(
                jobSeekerProfile.JobSeekerProfileId,
                employerProfileId,
                model.Rating,
                model.Comment);

            if (review == null)
            {
                TempData["ErrorMessage"] = "Unable to submit review. You may not be eligible or have already submitted a review.";
                return RedirectToAction("Index", new { employerProfileId });
            }

            TempData["SuccessMessage"] = "Your review has been submitted successfully!";
            return RedirectToAction("Index", new { employerProfileId });
        }

        /// <summary>
        /// POST: CompanyReviews/Report/{reviewId}
        /// Reports a review. Only for the employer who owns the company.
        /// </summary>
        [Authorize(Roles = "Employer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CompanyReviews/Report/{reviewId}")]
        public async Task<IActionResult> Report(int reviewId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var review = await _reviewService.GetReviewByIdAsync(reviewId);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Review not found.";
                return RedirectToAction("Index", "Companies");
            }

            var success = await _reviewService.ReportReviewAsync(reviewId, currentUserId.Value);

            if (success)
            {
                TempData["SuccessMessage"] = "Review has been reported and is now under review.";
            }
            else
            {
                TempData["ErrorMessage"] = "Unable to report review. You may not be authorized or the review has already been reported.";
            }

            return RedirectToAction("Index", new { employerProfileId = review.EmployerProfileId });
        }

        /// <summary>
        /// GET: CompanyReviews/Respond/{reviewId}
        /// Shows the response form. Only for the original reviewer when review is under review.
        /// </summary>
        [Authorize(Roles = "JobSeeker")]
        [HttpGet]
        [Route("CompanyReviews/Respond/{reviewId}")]
        public async Task<IActionResult> Respond(int reviewId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var review = await _reviewService.GetReviewByIdAsync(reviewId);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Review not found.";
                return RedirectToAction("Index", "Companies");
            }

            var jobSeekerProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == currentUserId.Value);

            if (jobSeekerProfile == null || review.JobSeekerProfileId != jobSeekerProfile.JobSeekerProfileId)
            {
                TempData["ErrorMessage"] = "You are not authorized to respond to this review.";
                return RedirectToAction("Index", new { employerProfileId = review.EmployerProfileId });
            }

            if (review.Status != ReviewStatus.UnderReview)
            {
                TempData["ErrorMessage"] = "This review is not under review.";
                return RedirectToAction("Index", new { employerProfileId = review.EmployerProfileId });
            }

            if (review.JobseekerResponded)
            {
                TempData["ErrorMessage"] = "You have already responded to this review report.";
                return RedirectToAction("Index", new { employerProfileId = review.EmployerProfileId });
            }

            var viewModel = new RespondToReportViewModel
            {
                ReviewId = reviewId,
                CompanyName = review.EmployerProfile.CompanyName,
                OriginalComment = review.Comment,
                Rating = review.Rating,
                ReportedAt = review.ReportedAt
            };

            return View(viewModel);
        }

        /// <summary>
        /// POST: CompanyReviews/Respond/{reviewId}
        /// Submits a response to a review report. Only for the original reviewer.
        /// </summary>
        [Authorize(Roles = "JobSeeker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CompanyReviews/Respond/{reviewId}")]
        public async Task<IActionResult> Respond(int reviewId, RespondToReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var jobSeekerProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == currentUserId.Value);

            if (jobSeekerProfile == null)
            {
                TempData["ErrorMessage"] = "JobSeeker profile not found.";
                return RedirectToAction("Index", "Companies");
            }

            var review = await _reviewService.GetReviewByIdAsync(reviewId);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Review not found.";
                return RedirectToAction("Index", "Companies");
            }

            var success = await _reviewService.RespondToReportAsync(
                reviewId,
                jobSeekerProfile.JobSeekerProfileId,
                model.ResponseComment);

            if (success)
            {
                TempData["SuccessMessage"] = "Your response has been submitted. The review will remain visible.";
            }
            else
            {
                TempData["ErrorMessage"] = "Unable to submit response.";
            }

            return RedirectToAction("Index", new { employerProfileId = review.EmployerProfileId });
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        #endregion
    }
}



