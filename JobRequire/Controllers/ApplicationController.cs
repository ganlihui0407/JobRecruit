using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Services;
using JobRequire.ViewModels;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Unified controller for application-related actions accessible by both Employer and JobSeeker.
    /// </summary>
    [Authorize]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IChatService _chatService;
        private readonly ILogger<ApplicationController> _logger;

        public ApplicationController(
            ApplicationDbContext context,
            IChatService chatService,
            ILogger<ApplicationController> logger)
        {
            _context = context;
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Application/Chat/{applicationId}
        /// Unified chat page accessible by both Employer and JobSeeker based on ownership.
        /// </summary>
        [HttpGet]
        [Route("Application/Chat/{applicationId}")]
        public async Task<IActionResult> Chat(int applicationId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            // Load the job application with necessary related data
            var application = await _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(j => j.User)
                .Include(a => a.JobVacancy)
                    .ThenInclude(j => j.EmployerProfile)
                .FirstOrDefaultAsync(a => a.JobApplicationId == applicationId);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index", "Home");
            }

            // Check access based on ownership logic (not role-based)
            bool hasAccess = false;
            string viewerRole = "";

            // Check if current user is the JobSeeker who owns this application
            if (application.JobSeekerProfile.UserId == userId)
            {
                hasAccess = true;
                viewerRole = "JobSeeker";
            }
            // Check if current user is the Employer who owns the job posting
            else if (application.JobVacancy?.EmployerProfile?.UserId == userId)
            {
                hasAccess = true;
                viewerRole = "Employer";
            }
            // Admin has access to all
            else if (userRole == "Admin")
            {
                hasAccess = true;
                viewerRole = "Admin";
            }

            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "You do not have access to this chat.";
                return View("AccessDenied");
            }

            // Mark messages as read for the current user
            await _chatService.MarkThreadAsReadAsync(applicationId, viewerRole);

            // Build view model
            var viewModel = new ApplicationChatViewModel
            {
                JobApplicationId = application.JobApplicationId,
                JobTitle = application.JobVacancy?.JobTitle ?? "Unknown Position",
                CompanyName = application.JobVacancy?.EmployerProfile?.CompanyName ?? "Unknown Company",
                ApplicantName = application.JobSeekerProfile.FullName,
                ApplicationStatus = application.Status,
                ApplyDate = application.ApplyDate,
                ViewerRole = viewerRole,
                // Provide back link based on viewer role
                BackUrl = viewerRole switch
                {
                    "JobSeeker" => Url.Action("MyApplicationDetails", "JobApplications", new { id = applicationId }),
                    "Employer" => Url.Action("Details", "JobApplications", new { id = applicationId }),
                    _ => Url.Action("Inbox", "ChatInbox")
                }
            };

            return View(viewModel);
        }

        /// <summary>
        /// Access Denied view for unauthorized chat access attempts.
        /// </summary>
        public IActionResult AccessDenied()
        {
            return View();
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

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}





