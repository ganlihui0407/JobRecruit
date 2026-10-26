using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.ViewModels;
using JobRequire.Services;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    [Authorize]
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly IResumeCompletionService _completionService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<JobApplicationsController> _logger;

        public JobApplicationsController(
            ApplicationDbContext context, 
            IWebHostEnvironment environment,
            IFileUploadService fileUploadService,
            INotificationService notificationService,
            IEmailSender emailSender,
            IResumeCompletionService completionService,
            IPdfService pdfService,
            ILogger<JobApplicationsController> logger)
        {
            _context = context;
            _environment = environment;
            _fileUploadService = fileUploadService;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _completionService = completionService;
            _pdfService = pdfService;
            _logger = logger;
        }

        // GET: JobApplications (For Employers to review applicants)
        [Authorize(Roles = "Employer,Admin")]
        public async Task<IActionResult> Index(
            int? jobVacancyId,
            string? statusFilter,
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy = "ApplyDate",
            string sortOrder = "desc",
            int pageNumber = 1,
            int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var employerProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employerProfile == null && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Please complete your company profile first.";
                return RedirectToAction("Create", "EmployerProfiles");
            }

            var query = _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(j => j.User)
                .Include(a => a.JobVacancy)
                    .ThenInclude(j => j.EmployerProfile)
                .Include(a => a.ApplicationDocuments)
                .AsQueryable();

            // Filter by employer's jobs (unless admin)
            if (!User.IsInRole("Admin") && employerProfile != null)
            {
                query = query.Where(a => a.JobVacancy.EmployerProfileId == employerProfile.EmployerProfileId);
            }

            // Apply filters
            if (jobVacancyId.HasValue)
            {
                query = query.Where(a => a.JobVacancyId == jobVacancyId);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a => a.JobSeekerProfile.FullName.Contains(searchTerm) ||
                                         a.JobSeekerProfile.User.Email.Contains(searchTerm) ||
                                         a.JobVacancy.JobTitle.Contains(searchTerm));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.ApplyDate >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.ApplyDate <= toDate);
            }

            // Apply sorting
            query = sortBy switch
            {
                "ApplicantName" => sortOrder == "asc" ? query.OrderBy(a => a.JobSeekerProfile.FullName) : query.OrderByDescending(a => a.JobSeekerProfile.FullName),
                "JobTitle" => sortOrder == "asc" ? query.OrderBy(a => a.JobVacancy.JobTitle) : query.OrderByDescending(a => a.JobVacancy.JobTitle),
                "Status" => sortOrder == "asc" ? query.OrderBy(a => a.Status) : query.OrderByDescending(a => a.Status),
                _ => sortOrder == "asc" ? query.OrderBy(a => a.ApplyDate) : query.OrderByDescending(a => a.ApplyDate)
            };

            var totalCount = await query.CountAsync();

            var applications = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new JobApplicationViewModel
                {
                    JobApplicationId = a.JobApplicationId,
                    JobSeekerProfileId = a.JobSeekerProfileId,
                    JobVacancyId = a.JobVacancyId,
                    ApplyDate = a.ApplyDate,
                    Status = a.Status,
                    ApplicantName = a.JobSeekerProfile.FullName,
                    ApplicantEmail = a.JobSeekerProfile.User.Email,
                    ApplicantPhone = a.JobSeekerProfile.PhoneNumber,
                    ApplicantPhoto = a.JobSeekerProfile.ProfilePhoto,
                    JobTitle = a.JobVacancy.JobTitle,
                    CompanyName = a.JobVacancy.EmployerProfile.CompanyName,
                    Documents = a.ApplicationDocuments.Select(d => new ApplicationDocumentViewModel
                    {
                        ApplicationDocumentId = d.ApplicationDocumentId,
                        FilePath = d.FilePath,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        ContentType = d.ContentType,
                        UploadedAt = d.UploadedAt,
                        DownloadUrl = Url.Action("Download", "Files", new { id = d.ApplicationDocumentId })!
                    }).ToList()
                })
                .ToListAsync();

            // Get filter options
            var jobVacancies = employerProfile != null
                ? await _context.JobVacancies
                    .Where(j => j.EmployerProfileId == employerProfile.EmployerProfileId)
                    .Select(j => new SelectListItem(j.JobTitle, j.JobVacancyId.ToString()))
                    .ToListAsync()
                : await _context.JobVacancies
                    .Select(j => new SelectListItem(j.JobTitle, j.JobVacancyId.ToString()))
                    .ToListAsync();

            var viewModel = new JobApplicationListViewModel
            {
                Applications = applications,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                JobVacancyId = jobVacancyId,
                StatusFilter = statusFilter,
                SearchTerm = searchTerm,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy,
                SortOrder = sortOrder,
                JobVacancies = jobVacancies,
                Statuses = ApplicationStatusOptions.GetStatuses(),
                CurrentJobTitle = jobVacancyId.HasValue
                    ? await _context.JobVacancies.Where(j => j.JobVacancyId == jobVacancyId).Select(j => j.JobTitle).FirstOrDefaultAsync()
                    : null
            };

            return View(viewModel);
        }

        // GET: JobApplications/Details/5 (View resume snapshot and documents)
        [Authorize(Roles = "Employer,Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(j => j.User)
                .Include(a => a.JobSeekerProfile.ResumeEducations)
                .Include(a => a.JobSeekerProfile.ResumeExperiences)
                .Include(a => a.JobSeekerProfile.ResumeSkills)
                .Include(a => a.JobVacancy)
                    .ThenInclude(j => j.EmployerProfile)
                .Include(a => a.ApplicationDocuments)
                .Include(a => a.StatusHistory)
                .Include(a => a.InterviewDetails)
                .Include(a => a.OfferDetails)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin"))
            {
                var employerProfile = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employerProfile == null || application.JobVacancy.EmployerProfileId != employerProfile.EmployerProfileId)
                {
                    return Forbid();
                }
            }

            var viewModel = new JobApplicationViewModel
            {
                JobApplicationId = application.JobApplicationId,
                JobSeekerProfileId = application.JobSeekerProfileId,
                JobVacancyId = application.JobVacancyId,
                ApplyDate = application.ApplyDate,
                Status = application.Status,
                ResumeSnapshot = application.ResumeSnapshot,
                AdditionalDocument = application.AdditionalDocument,
                // Resume submission fields
                ResumeSource = application.ResumeSource ?? ResumeSourceType.Website,
                ResumeFilePath = application.ResumeFilePath,
                // Applicant info
                ApplicantUserId = application.JobSeekerProfile.UserId,
                ApplicantName = application.JobSeekerProfile.FullName,
                ApplicantEmail = application.JobSeekerProfile.User.Email,
                ApplicantPhone = application.JobSeekerProfile.PhoneNumber,
                ApplicantPhoto = application.JobSeekerProfile.ProfilePhoto,
                ApplicantSummary = application.JobSeekerProfile.Summary,
                JobTitle = application.JobVacancy.JobTitle,
                CompanyName = application.JobVacancy.EmployerProfile.CompanyName,
                JobLocation = application.JobVacancy.Location,
                Education = application.JobSeekerProfile.ResumeEducations
                    .OrderByDescending(e => e.EndYear)
                    .Select(e => new ResumeEducationViewModel
                    {
                        ResumeEducationId = e.ResumeEducationId,
                        SchoolName = e.SchoolName,
                        Qualification = e.Qualification,
                        FieldOfStudy = e.FieldOfStudy,
                        StartYear = e.StartYear,
                        EndYear = e.EndYear
                    }).ToList(),
                Experience = application.JobSeekerProfile.ResumeExperiences
                    .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                    .Select(e => new ResumeExperienceViewModel
                    {
                        ResumeExperienceId = e.ResumeExperienceId,
                        CompanyName = e.CompanyName,
                        JobTitle = e.JobTitle,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Description = e.Description
                    }).ToList(),
                Skills = application.JobSeekerProfile.ResumeSkills
                    .Select(s => new ResumeSkillViewModel
                    {
                        ResumeSkillId = s.ResumeSkillId,
                        SkillName = s.SkillName,
                        SkillLevel = s.SkillLevel
                    }).ToList(),
                Documents = application.ApplicationDocuments
                    .Select(d => new ApplicationDocumentViewModel
                    {
                        ApplicationDocumentId = d.ApplicationDocumentId,
                        JobApplicationId = d.JobApplicationId,
                        FilePath = d.FilePath,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        ContentType = d.ContentType,
                        UploadedAt = d.UploadedAt,
                        DownloadUrl = Url.Action("Download", "Files", new { id = d.ApplicationDocumentId })!
                    }).ToList(),
                StatusOptions = ApplicationStatusOptions.GetStatuses(),
                Timeline = BuildApplicationTimeline(application)
            };

            return View(viewModel);
        }

        // GET: JobApplications/ViewResume/5
        /// <summary>
        /// View resume for a job application - serves PDF or redirects to website resume
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Employer,Admin")]
        public async Task<IActionResult> ViewResume(int id)
        {
            var application = await _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                .Include(a => a.JobVacancy)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Check authorization - employer must own the job
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin"))
            {
                var employerProfile = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employerProfile == null || application.JobVacancy.EmployerProfileId != employerProfile.EmployerProfileId)
                {
                    return Forbid();
                }
            }

            // Handle based on resume source
            if (application.ResumeSource == ResumeSourceType.PDF)
            {
                // Serve PDF file
                if (string.IsNullOrEmpty(application.ResumeFilePath))
                {
                    return NotFound("Resume file not found.");
                }

                // Get file stream from the file upload service (handles protected files path)
                var fileStream = await _fileUploadService.GetFileStreamAsync(application.ResumeFilePath);
                
                if (fileStream == null)
                {
                    _logger.LogWarning("Resume file not found at path: {FilePath}", application.ResumeFilePath);
                    return NotFound("Resume file not found on server.");
                }

                var fileName = Path.GetFileName(application.ResumeFilePath);
                return File(fileStream, "application/pdf", fileName);
            }
            else if (application.ResumeSource == ResumeSourceType.Website)
            {
                // Redirect to website resume view
                return RedirectToAction("View", "Resumes", new { jobSeekerId = application.JobSeekerProfile.JobSeekerProfileId });
            }
            else
            {
                return NotFound("No resume attached to this application.");
            }
        }

        // POST: JobApplications/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "Employer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id, 
            string status, 
            string reason,
            // Interview fields
            DateTime? interviewDate,
            string? interviewTime,
            string? locationOrLink,
            string? interviewContactPerson,
            string? interviewContactPhone,
            string? interviewNotes,
            // Offer fields
            string? offerJobTitle,
            decimal? salaryOffered,
            DateTime? startDate,
            string? employmentType,
            string? offerContactPerson,
            string? offerContactPhone,
            string? offerNotes)
        {
            // Check if application is already rejected (final status)
            var currentApplication = await _context.JobApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (currentApplication != null && currentApplication.Status == "Rejected")
            {
                TempData["ErrorMessage"] = "This application has been rejected and can no longer be updated.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate reason is provided
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Please provide a reason for the status update.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate Interview fields if status is Interview
            if (status == "Interview")
            {
                if (!interviewDate.HasValue || string.IsNullOrWhiteSpace(interviewTime) ||
                    string.IsNullOrWhiteSpace(locationOrLink) || string.IsNullOrWhiteSpace(interviewContactPerson) ||
                    string.IsNullOrWhiteSpace(interviewContactPhone))
                {
                    TempData["ErrorMessage"] = "Please fill in all required interview details.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            // Validate Offer fields if status is Offered
            if (status == "Offered")
            {
                if (string.IsNullOrWhiteSpace(offerJobTitle) || !salaryOffered.HasValue ||
                    !startDate.HasValue || string.IsNullOrWhiteSpace(employmentType) ||
                    string.IsNullOrWhiteSpace(offerContactPerson) || string.IsNullOrWhiteSpace(offerContactPhone))
                {
                    TempData["ErrorMessage"] = "Please fill in all required offer details.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            var application = await _context.JobApplications
                .Include(a => a.JobVacancy)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin"))
            {
                var employerProfile = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employerProfile == null || application.JobVacancy.EmployerProfileId != employerProfile.EmployerProfileId)
                {
                    return Forbid();
                }
            }

            var oldStatus = application.Status;
            application.Status = status;
            
            // Create status history record
            var statusHistory = new ApplicationStatusHistory
            {
                JobApplicationId = application.JobApplicationId,
                Status = status,
                Reason = reason.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedByEmployerId = userId!.Value
            };
            _context.ApplicationStatusHistories.Add(statusHistory);

            // Variables to store PDF data for email attachment
            byte[]? pdfBytes = null;
            string? pdfFileName = null;
            InterviewDetails? interview = null;
            OfferDetails? offer = null;

            // Save Interview details if status is Interview
            if (status == "Interview" && interviewDate.HasValue)
            {
                interview = new InterviewDetails
                {
                    JobApplicationId = application.JobApplicationId,
                    InterviewDate = interviewDate.Value,
                    InterviewTime = interviewTime!,
                    LocationOrLink = locationOrLink!,
                    ContactPerson = interviewContactPerson!,
                    ContactPhone = interviewContactPhone!,
                    Notes = interviewNotes,
                    CreatedAt = DateTime.UtcNow
                };
                _context.InterviewDetails.Add(interview);
            }

            // Save Offer details if status is Offered
            if (status == "Offered" && salaryOffered.HasValue)
            {
                offer = new OfferDetails
                {
                    JobApplicationId = application.JobApplicationId,
                    JobTitle = offerJobTitle!,
                    SalaryOffered = salaryOffered.Value,
                    StartDate = startDate!.Value,
                    EmploymentType = employmentType!,
                    ContactPerson = offerContactPerson!,
                    ContactPhone = offerContactPhone!,
                    Notes = offerNotes,
                    CreatedAt = DateTime.UtcNow
                };
                _context.OfferDetails.Add(offer);
            }
            
            await _context.SaveChangesAsync();

            // Get job seeker info for notification
            var applicationWithSeeker = await _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(s => s.User)
                .Include(a => a.JobVacancy)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            // Create notification for job seeker (status changed)
            if (applicationWithSeeker?.JobSeekerProfile?.User != null && oldStatus != status)
            {
                var notificationType = status switch
                {
                    "Shortlisted" or "Interview" => "Success",
                    "Rejected" => "Error",
                    "Hired" => "Success",
                    _ => "Info"
                };

                var statusMessage = status switch
                {
                    "Shortlisted" => "Congratulations! You have been shortlisted.",
                    "Interview" => "You have been selected for an interview!",
                    "Rejected" => "Unfortunately, your application was not selected.",
                    "Hired" => "Congratulations! You have been hired!",
                    _ => $"Your application status has been updated to {status}."
                };

                await _notificationService.CreateNotificationWithActivityAsync(
                    applicationWithSeeker.JobSeekerProfile.User.UserId,
                    $"Application Status Updated: {status}",
                    $"{statusMessage} Position: {applicationWithSeeker.JobVacancy?.JobTitle}",
                    notificationType,
                    Url.Action("MyApplicationDetails", "JobApplications", new { id }),
                    "View Application",
                    "ApplicationStatusChanged",
                    $"Application status changed from {oldStatus} to {status}"
                );

                // Send email notification to job seeker about status change (does not affect status update)
                try
                {
                    // Get employer info for the email
                    var jobWithEmployer = await _context.JobVacancies
                        .Include(j => j.EmployerProfile)
                        .FirstOrDefaultAsync(j => j.JobVacancyId == applicationWithSeeker.JobVacancyId);

                    var applicationsLink = Url.Action("MyApplications", "JobApplications", null, Request.Scheme);
                    var companyName = jobWithEmployer?.EmployerProfile?.CompanyName ?? "Unknown Company";
                    var companyAddress = jobWithEmployer?.EmployerProfile?.CompanyAddress;
                    var jobSeekerName = applicationWithSeeker.JobSeekerProfile.FullName;
                    var jobTitle = applicationWithSeeker.JobVacancy?.JobTitle ?? "Unknown Position";

                    // Generate PDF and send email with attachment for Interview or Offered status
                    if (status == "Interview" && interview != null)
                    {
                        // Generate Interview PDF
                        pdfBytes = _pdfService.GenerateInterviewPdf(interview, jobSeekerName, jobTitle, companyName, companyAddress);
                        pdfFileName = $"Interview_Invitation_{jobSeekerName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";

                        // Save PDF path to database
                        if (pdfBytes != null && pdfBytes.Length > 0)
                        {
                            var pdfPath = await SavePdfToFileAsync(pdfFileName, pdfBytes);
                            interview.PdfPath = pdfPath;
                            await _context.SaveChangesAsync();

                            // Send email with attachment
                            await _emailSender.SendApplicationStatusUpdateWithAttachmentAsync(
                                applicationWithSeeker.JobSeekerProfile.User.Email,
                                jobSeekerName,
                                jobTitle,
                                companyName,
                                status,
                                applicationsLink!,
                                pdfBytes,
                                pdfFileName
                            );
                            _logger.LogInformation("Interview invitation email with PDF sent to job seeker for application {Id}", id);
                        }
                        else
                        {
                            // Fallback to regular email if PDF generation failed
                            await _emailSender.SendApplicationStatusUpdateAsync(
                                applicationWithSeeker.JobSeekerProfile.User.Email,
                                jobSeekerName,
                                jobTitle,
                                companyName,
                                status,
                                applicationsLink!
                            );
                        }
                    }
                    else if (status == "Offered" && offer != null)
                    {
                        // Generate Offer PDF
                        pdfBytes = _pdfService.GenerateOfferPdf(offer, jobSeekerName, companyName, companyAddress);
                        pdfFileName = $"Offer_Letter_{jobSeekerName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";

                        // Save PDF path to database
                        if (pdfBytes != null && pdfBytes.Length > 0)
                        {
                            var pdfPath = await SavePdfToFileAsync(pdfFileName, pdfBytes);
                            offer.PdfPath = pdfPath;
                            await _context.SaveChangesAsync();

                            // Send email with attachment
                            await _emailSender.SendApplicationStatusUpdateWithAttachmentAsync(
                                applicationWithSeeker.JobSeekerProfile.User.Email,
                                jobSeekerName,
                                jobTitle,
                                companyName,
                                status,
                                applicationsLink!,
                                pdfBytes,
                                pdfFileName
                            );
                            _logger.LogInformation("Offer letter email with PDF sent to job seeker for application {Id}", id);
                        }
                        else
                        {
                            // Fallback to regular email if PDF generation failed
                            await _emailSender.SendApplicationStatusUpdateAsync(
                                applicationWithSeeker.JobSeekerProfile.User.Email,
                                jobSeekerName,
                                jobTitle,
                                companyName,
                                status,
                                applicationsLink!
                            );
                        }
                    }
                    else
                    {
                        // Regular email for other statuses
                        await _emailSender.SendApplicationStatusUpdateAsync(
                            applicationWithSeeker.JobSeekerProfile.User.Email,
                            jobSeekerName,
                            jobTitle,
                            companyName,
                            status,
                            applicationsLink!
                        );
                    }
                    _logger.LogInformation("Status update email sent to job seeker for application {Id}", id);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the status update
                    _logger.LogWarning(ex, "Failed to send status update email to job seeker");
                }
            }

            TempData["SuccessMessage"] = $"Application status updated to {status}!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: JobApplications/MyApplications (For Job Seekers)
        [Authorize(Roles = "JobSeeker,Admin")]
        public async Task<IActionResult> MyApplications(
            string? statusFilter,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var jobSeekerProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(j => j.UserId == userId);

            if (jobSeekerProfile == null)
            {
                TempData["ErrorMessage"] = "Please complete your profile first.";
                return RedirectToAction("Create", "JobSeekerProfiles");
            }

            var query = _context.JobApplications
                .Include(a => a.JobVacancy)
                    .ThenInclude(j => j!.EmployerProfile)
                .Where(a => a.JobSeekerProfileId == jobSeekerProfile.JobSeekerProfileId);

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            var totalCount = await query.CountAsync();

            var applications = await query
                .Where(a => a.JobVacancy != null && a.JobVacancy.EmployerProfile != null)
                .OrderByDescending(a => a.ApplyDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new JobApplicationViewModel
                {
                    JobApplicationId = a.JobApplicationId,
                    JobSeekerProfileId = a.JobSeekerProfileId,
                    JobVacancyId = a.JobVacancyId,
                    ApplyDate = a.ApplyDate,
                    Status = a.Status ?? "Pending",
                    JobTitle = a.JobVacancy!.JobTitle ?? "Unknown Job",
                    CompanyName = a.JobVacancy.EmployerProfile!.CompanyName ?? "Unknown Company",
                    JobLocation = a.JobVacancy.Location ?? "Not specified"
                })
                .ToListAsync();

            var viewModel = new JobApplicationListViewModel
            {
                Applications = applications,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                StatusFilter = statusFilter,
                Statuses = ApplicationStatusOptions.GetStatuses()
            };

            return View(viewModel);
        }

        // GET: JobApplications/MyApplicationDetails/5 (For Job Seekers to view their application details)
        [Authorize(Roles = "JobSeeker,Admin")]
        public async Task<IActionResult> MyApplicationDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var application = await _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(j => j.User)
                .Include(a => a.JobSeekerProfile.ResumeEducations)
                .Include(a => a.JobSeekerProfile.ResumeExperiences)
                .Include(a => a.JobSeekerProfile.ResumeSkills)
                .Include(a => a.JobVacancy)
                    .ThenInclude(j => j!.EmployerProfile)
                .Include(a => a.ApplicationDocuments)
                .Include(a => a.StatusHistory)
                .Include(a => a.InterviewDetails)
                .Include(a => a.OfferDetails)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            // Verify ownership - jobseeker can only view their own applications
            if (!User.IsInRole("Admin") && application.JobSeekerProfile.UserId != userId)
            {
                return NotFound(); // Return NotFound instead of Forbid for security
            }

            // Get the latest status history record
            var latestStatusHistory = application.StatusHistory
                .OrderByDescending(sh => sh.CreatedAt)
                .FirstOrDefault();

            var viewModel = new JobSeekerApplicationDetailsViewModel
            {
                JobApplicationId = application.JobApplicationId,
                JobVacancyId = application.JobVacancyId,
                JobTitle = application.JobVacancy?.JobTitle ?? "Unknown Position",
                CompanyName = application.JobVacancy?.EmployerProfile?.CompanyName ?? "Unknown Company",
                CompanyLogo = application.JobVacancy?.EmployerProfile?.CompanyLogo,
                Location = application.JobVacancy?.Location,
                JobType = application.JobVacancy?.JobType,
                SalaryMin = application.JobVacancy?.SalaryMin,
                SalaryMax = application.JobVacancy?.SalaryMax,
                Status = application.Status ?? "Pending",
                ApplyDate = application.ApplyDate,
                LatestStatusReason = latestStatusHistory?.Reason,
                LatestStatusUpdatedAt = latestStatusHistory?.CreatedAt,
                ResumeSource = application.ResumeSource ?? ResumeSourceType.Website,
                ResumeFilePath = application.ResumeFilePath,
                ResumeSnapshot = application.ResumeSnapshot,
                ApplicantName = application.JobSeekerProfile.FullName,
                ApplicantEmail = application.JobSeekerProfile.User?.Email,
                ApplicantPhone = application.JobSeekerProfile.PhoneNumber,
                ApplicantSummary = application.JobSeekerProfile.Summary,
                Education = application.JobSeekerProfile.ResumeEducations
                    .OrderByDescending(e => e.EndYear ?? 9999)
                    .Select(e => new ResumeEducationViewModel
                    {
                        ResumeEducationId = e.ResumeEducationId,
                        SchoolName = e.SchoolName,
                        Qualification = e.Qualification,
                        FieldOfStudy = e.FieldOfStudy,
                        StartYear = e.StartYear,
                        EndYear = e.EndYear
                    }).ToList(),
                Experience = application.JobSeekerProfile.ResumeExperiences
                    .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                    .Select(e => new ResumeExperienceViewModel
                    {
                        ResumeExperienceId = e.ResumeExperienceId,
                        CompanyName = e.CompanyName,
                        JobTitle = e.JobTitle,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Description = e.Description
                    }).ToList(),
                Skills = application.JobSeekerProfile.ResumeSkills
                    .Select(s => new ResumeSkillViewModel
                    {
                        ResumeSkillId = s.ResumeSkillId,
                        SkillName = s.SkillName,
                        SkillLevel = s.SkillLevel
                    }).ToList(),
                Documents = application.ApplicationDocuments
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new ApplicationDocumentViewModel
                    {
                        ApplicationDocumentId = d.ApplicationDocumentId,
                        JobApplicationId = d.JobApplicationId,
                        FilePath = d.FilePath,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        ContentType = d.ContentType,
                        UploadedAt = d.UploadedAt,
                        DownloadUrl = Url.Action("Download", "Files", new { id = d.ApplicationDocumentId }) ?? "#"
                    }).ToList(),
                Timeline = BuildApplicationTimeline(application)
            };

            return View(viewModel);
        }

        // GET: JobApplications/Apply/{id} - Show application form
        [HttpGet]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Apply(int id)
        {
            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .FirstOrDefaultAsync(j => j.JobVacancyId == id);

            if (job == null || job.Status != "Open")
            {
                TempData["ErrorMessage"] = "This job is no longer available.";
                return RedirectToAction("Index", "Jobs");
            }

            // Check if already applied
            var userId = GetCurrentUserId();
            var jobSeekerProfile = await _context.JobSeekerProfiles
                .Include(j => j.ResumeEducations)
                .Include(j => j.ResumeExperiences)
                .Include(j => j.ResumeSkills)
                .FirstOrDefaultAsync(j => j.UserId == userId);

            if (jobSeekerProfile == null)
            {
                TempData["ErrorMessage"] = "Please complete your profile before applying.";
                return RedirectToAction("Create", "JobSeekerProfiles");
            }

            // Check if THIS user has already applied to THIS job with a non-withdrawn status
            // This check is per-user: other jobseekers can still apply to the same job
            // Withdrawn applications do NOT block re-application
            var existingApplication = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobSeekerProfileId == jobSeekerProfile.JobSeekerProfileId &&
                                          a.JobVacancyId == id &&
                                          a.Status != "Withdrawn");

            if (existingApplication != null)
            {
                TempData["ErrorMessage"] = "You have already applied for this job.";
                return RedirectToAction("Details", "Jobs", new { id = id });
            }

            // Build website resume summary for display (but do NOT auto-select)
            var websiteResumeSummary = BuildWebsiteResumeSummary(jobSeekerProfile);

            // Build Resume Fit Hint for informational display
            var fitHint = BuildResumeFitHint(jobSeekerProfile, job.Requirements);
            
            // Build the view model with resume summary
            // IMPORTANT: Do NOT auto-select any resume option - user must always choose explicitly
            // ResumeSource is left empty/null to force user selection
            var viewModel = new ApplyJobViewModel
            {
                JobVacancyId = id,
                JobTitle = job.JobTitle,
                CompanyName = job.EmployerProfile.CompanyName,
                CompanyLogo = job.EmployerProfile.CompanyLogo,
                Location = job.Location,
                JobType = job.JobType,
                ResumeSource = null, // Force user to make explicit selection - no auto-select
                WebsiteResume = websiteResumeSummary,
                FitHint = fitHint
            };

            return View(viewModel);
        }

        /// <summary>
        /// Build a summary of the job seeker's website resume
        /// </summary>
        private WebsiteResumeSummary BuildWebsiteResumeSummary(JobSeekerProfile profile)
        {
            var latestEducation = profile.ResumeEducations
                .OrderByDescending(e => e.EndYear ?? 9999)
                .FirstOrDefault();

            var latestExperience = profile.ResumeExperiences
                .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                .FirstOrDefault();

            var topSkills = profile.ResumeSkills
                .Take(5)
                .Select(s => s.SkillName)
                .ToList();

            // Use the shared completion service for consistent calculation
            var completionPercentage = _completionService.CalculateCompletion(profile);

            return new WebsiteResumeSummary
            {
                JobSeekerProfileId = profile.JobSeekerProfileId,
                FullName = profile.FullName,
                Summary = profile.Summary,
                EducationCount = profile.ResumeEducations.Count,
                ExperienceCount = profile.ResumeExperiences.Count,
                SkillsCount = profile.ResumeSkills.Count,
                LatestEducation = latestEducation != null
                    ? $"{latestEducation.Qualification} in {latestEducation.FieldOfStudy} - {latestEducation.SchoolName}"
                    : null,
                LatestExperience = latestExperience != null
                    ? $"{latestExperience.JobTitle} at {latestExperience.CompanyName}"
                    : null,
                TopSkills = topSkills,
                CompletionPercentage = completionPercentage
            };
        }

        /// <summary>
        /// Build Resume-Job Fit Hint by comparing job requirements with resume skills
        /// This is informational only and does not block application submission
        /// </summary>
        private ResumeFitHint BuildResumeFitHint(JobSeekerProfile profile, string? jobRequirements)
        {
            var hint = new ResumeFitHint();

            // Check if website resume exists (at least one section filled)
            bool hasEducation = profile.ResumeEducations?.Any() == true;
            bool hasExperience = profile.ResumeExperiences?.Any() == true;
            bool hasSkills = profile.ResumeSkills?.Any() == true;

            hint.HasWebsiteResume = hasEducation || hasExperience || hasSkills;

            // If no website resume, return early without match level
            if (!hint.HasWebsiteResume)
            {
                return hint;
            }

            // If no job requirements, cannot calculate match
            if (string.IsNullOrWhiteSpace(jobRequirements))
            {
                hint.MatchLevel = "Low Match"; // Default when no requirements to compare
                return hint;
            }

            // Get user skills and convert to lowercase for comparison
            var userSkills = profile.ResumeSkills?
                .Select(s => s.SkillName?.ToLowerInvariant() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();

            if (!userSkills.Any())
            {
                hint.MatchLevel = "Low Match";
                // Extract required skills to show as missing when user has no skills
                hint.MissingSkills = ExtractSkillsFromRequirements(jobRequirements);
                return hint;
            }

            // Convert job requirements to lowercase for comparison
            var requirementsLower = jobRequirements.ToLowerInvariant();

            // Count matched skills using simple contains check
            var matchedSkills = userSkills
                .Where(skill => requirementsLower.Contains(skill))
                .ToList();

            hint.MatchedSkillsCount = matchedSkills.Count;
            hint.MatchedSkills = profile.ResumeSkills?
                .Where(s => matchedSkills.Contains(s.SkillName?.ToLowerInvariant() ?? ""))
                .Select(s => s.SkillName)
                .ToList() ?? new List<string>();

            // Determine match level based on count
            hint.MatchLevel = hint.MatchedSkillsCount switch
            {
                >= 3 => "Good Match",
                >= 1 => "Partial Match",
                _ => "Low Match"
            };

            // Extract and calculate missing skills for Partial/Low Match only
            if (hint.MatchLevel != "Good Match")
            {
                var requiredSkills = ExtractSkillsFromRequirements(jobRequirements);
                hint.MissingSkills = requiredSkills
                    .Where(reqSkill => !userSkills.Any(userSkill => 
                        reqSkill.ToLowerInvariant().Contains(userSkill) || 
                        userSkill.Contains(reqSkill.ToLowerInvariant())))
                    .Take(5) // Limit to top 5 missing skills for cleaner display
                    .ToList();
            }

            return hint;
        }

        /// <summary>
        /// Extract skill keywords from job requirements text
        /// Uses common technical skill patterns and filters out generic words
        /// </summary>
        private List<string> ExtractSkillsFromRequirements(string requirements)
        {
            if (string.IsNullOrWhiteSpace(requirements))
                return new List<string>();

            // Common technical skills and keywords to look for
            var commonSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Programming Languages
                "C#", "Java", "Python", "JavaScript", "TypeScript", "PHP", "Ruby", "Go", "Rust", "Swift", "Kotlin",
                "C++", "C", "Scala", "Perl", "R", "MATLAB", "VB.NET", "F#", "Dart", "Lua",
                // Web Technologies
                "HTML", "CSS", "React", "Angular", "Vue", "Vue.js", "Node.js", "Express", "Django", "Flask",
                "ASP.NET", "ASP.NET Core", ".NET", ".NET Core", "Spring", "Spring Boot", "Laravel", "Rails",
                "jQuery", "Bootstrap", "Tailwind", "SASS", "LESS", "Webpack", "Next.js", "Nuxt.js",
                // Databases
                "SQL", "MySQL", "PostgreSQL", "MongoDB", "Oracle", "SQL Server", "SQLite", "Redis", "Cassandra",
                "DynamoDB", "Firebase", "Elasticsearch", "MariaDB", "Neo4j", "CouchDB",
                // Cloud & DevOps
                "AWS", "Azure", "GCP", "Google Cloud", "Docker", "Kubernetes", "Jenkins", "Git", "GitHub",
                "GitLab", "CI/CD", "Terraform", "Ansible", "Linux", "Unix", "Nginx", "Apache",
                // Data & AI
                "Machine Learning", "Deep Learning", "AI", "TensorFlow", "PyTorch", "Pandas", "NumPy",
                "Data Analysis", "Data Science", "Big Data", "Hadoop", "Spark", "Tableau", "Power BI",
                // Mobile
                "Android", "iOS", "React Native", "Flutter", "Xamarin", "SwiftUI",
                // Other Technical
                "REST", "RESTful", "API", "GraphQL", "Microservices", "Agile", "Scrum", "JIRA",
                "Unit Testing", "TDD", "OOP", "Design Patterns", "MVC", "MVVM",
                // Soft Skills (commonly mentioned in requirements)
                "Communication", "Leadership", "Teamwork", "Problem Solving", "Analytical",
                // Business/Office
                "Excel", "Word", "PowerPoint", "SAP", "Salesforce", "CRM", "ERP",
                // Accounting/Finance
                "Accounting", "Financial Analysis", "Budgeting", "Auditing", "Tax", "GAAP", "IFRS",
                // Marketing
                "SEO", "SEM", "Google Analytics", "Digital Marketing", "Social Media", "Content Marketing"
            };

            var requirementsLower = requirements.ToLowerInvariant();
            var foundSkills = new List<string>();

            foreach (var skill in commonSkills)
            {
                // Check if skill exists in requirements (case-insensitive)
                if (requirementsLower.Contains(skill.ToLowerInvariant()))
                {
                    foundSkills.Add(skill);
                }
            }

            return foundSkills.Distinct().ToList();
        }

        // POST: JobApplications/Apply (For Job Seekers to apply)
        [HttpPost]
        [Authorize(Roles = "JobSeeker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyJobViewModel model)
        {
            var userId = GetCurrentUserId();
            var jobSeekerProfile = await _context.JobSeekerProfiles
                .Include(j => j.ResumeEducations)
                .Include(j => j.ResumeExperiences)
                .Include(j => j.ResumeSkills)
                .FirstOrDefaultAsync(j => j.UserId == userId);

            if (jobSeekerProfile == null)
            {
                TempData["ErrorMessage"] = "Please complete your profile before applying.";
                return RedirectToAction("Create", "JobSeekerProfiles");
            }

            var job = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .FirstOrDefaultAsync(j => j.JobVacancyId == model.JobVacancyId);

            if (job == null || job.Status != "Open")
            {
                TempData["ErrorMessage"] = "This job is no longer available.";
                return RedirectToAction("Index", "Jobs");
            }

            // Check if THIS user has already applied to THIS job with a non-withdrawn status
            // This check is per-user: other jobseekers can still apply to the same job
            // Withdrawn applications do NOT block re-application
            var existingApplication = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobSeekerProfileId == jobSeekerProfile.JobSeekerProfileId &&
                                          a.JobVacancyId == model.JobVacancyId &&
                                          a.Status != "Withdrawn");

            if (existingApplication != null)
            {
                TempData["ErrorMessage"] = "You have already applied for this job.";
                return RedirectToAction("Details", "JobVacancies", new { id = model.JobVacancyId });
            }

            // Rebuild view model for validation errors
            model.JobTitle = job.JobTitle;
            model.CompanyName = job.EmployerProfile.CompanyName;
            model.CompanyLogo = job.EmployerProfile.CompanyLogo;
            model.Location = job.Location;
            model.JobType = job.JobType;
            model.WebsiteResume = BuildWebsiteResumeSummary(jobSeekerProfile);

            // Validate resume source selection
            if (string.IsNullOrEmpty(model.ResumeSource))
            {
                ModelState.AddModelError("ResumeSource", "Please select a resume option.");
                return View(model);
            }

            string? resumeFilePath = null;
            string? resumeSnapshot = null;

            // Validate based on resume source
            if (model.ResumeSource == ResumeSourceType.PDF)
            {
                // Validate PDF upload
                if (model.ResumeFile == null || model.ResumeFile.Length == 0)
                {
                    ModelState.AddModelError("ResumeFile", "Please upload your resume (PDF file).");
                    return View(model);
                }

                // Validate file type
                var fileExtension = Path.GetExtension(model.ResumeFile.FileName).ToLowerInvariant();
                if (fileExtension != ".pdf")
                {
                    ModelState.AddModelError("ResumeFile", "Only PDF files are accepted for resume upload.");
                    return View(model);
                }

                // Validate file size (max 10MB)
                if (model.ResumeFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("ResumeFile", "Resume file size must not exceed 10MB.");
                    return View(model);
                }

                // Upload the resume file
                var uploadResult = await _fileUploadService.UploadFileAsync(model.ResumeFile, userId!.Value, "resumes");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("ResumeFile", $"Failed to upload resume: {uploadResult.ErrorMessage}");
                    return View(model);
                }

                resumeFilePath = uploadResult.FilePath;
                _logger.LogInformation($"Resume PDF uploaded for user {userId}: {uploadResult.FileName}");
            }
            else if (model.ResumeSource == ResumeSourceType.Website)
            {
                // Validate website resume has required content
                var hasEducation = jobSeekerProfile.ResumeEducations.Any();
                var hasExperience = jobSeekerProfile.ResumeExperiences.Any();

                if (!hasEducation && !hasExperience)
                {
                    ModelState.AddModelError("ResumeSource", "Your website resume must have at least 1 education or 1 work experience entry. Please complete your resume first.");
                    return View(model);
                }

                // Generate resume snapshot
                resumeSnapshot = GenerateResumeSnapshot(jobSeekerProfile);
            }
            else
            {
                ModelState.AddModelError("ResumeSource", "Invalid resume source selected.");
                return View(model);
            }

            // Create the application
            var application = new JobApplication
            {
                JobSeekerProfileId = jobSeekerProfile.JobSeekerProfileId,
                JobVacancyId = model.JobVacancyId,
                ApplyDate = DateTime.UtcNow,
                Status = "Pending",
                ResumeSource = model.ResumeSource,
                ResumeFilePath = resumeFilePath,
                ResumeSnapshot = resumeSnapshot
            };

            _context.Add(application);
            await _context.SaveChangesAsync();

            // Get job and employer info for notifications
            var jobWithEmployer = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(j => j.JobVacancyId == model.JobVacancyId);

            // Create notification for employer (new application received)
            if (jobWithEmployer?.EmployerProfile?.User != null)
            {
                await _notificationService.CreateNotificationWithActivityAsync(
                    jobWithEmployer.EmployerProfile.User.UserId,
                    "New Job Application Received",
                    $"{jobSeekerProfile.FullName} applied for '{jobWithEmployer.JobTitle}'",
                    "Info",
                    Url.Action("Details", "JobApplications", new { id = application.JobApplicationId }),
                    "View Application",
                    "NewApplicationReceived",
                    $"New application from {jobSeekerProfile.FullName} for {jobWithEmployer.JobTitle}"
                );

                // Send email notification to employer (does not affect application success)
                try
                {
                    var dashboardLink = Url.Action("Details", "JobApplications", 
                        new { id = application.JobApplicationId }, Request.Scheme);
                    
                    await _emailSender.SendJobApplicationNotificationAsync(
                        jobWithEmployer.EmployerProfile.User.Email,
                        jobWithEmployer.EmployerProfile.CompanyName,
                        jobWithEmployer.JobTitle,
                        jobSeekerProfile.FullName,
                        dashboardLink!
                    );
                    _logger.LogInformation($"Application notification email sent to employer for job {jobWithEmployer.JobTitle}");
                }
                catch (Exception ex)
                {
                    // Log but don't fail the application submission
                    _logger.LogWarning(ex, $"Failed to send application notification email to employer");
                }
            }

            // Create notification for job seeker (application submitted)
            await _notificationService.CreateNotificationWithActivityAsync(
                userId!.Value,
                "Application Submitted Successfully",
                $"Your application for '{jobWithEmployer?.JobTitle}' at {jobWithEmployer?.EmployerProfile?.CompanyName} has been submitted.",
                "Success",
                Url.Action("MyApplicationDetails", "JobApplications", new { id = application.JobApplicationId }),
                "View Application",
                "ApplicationSubmitted",
                $"Application submitted for {jobWithEmployer?.JobTitle}"
            );

            // Handle additional document uploads
            if (model.AdditionalDocuments != null && model.AdditionalDocuments.Count > 0)
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in model.AdditionalDocuments)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, userId!.Value, "applications");
                    
                    if (uploadResult.Success)
                    {
                        var document = new ApplicationDocument
                        {
                            JobApplicationId = application.JobApplicationId,
                            FilePath = uploadResult.FilePath!,
                            FileName = uploadResult.FileName!,
                            FileSize = uploadResult.FileSize,
                            ContentType = uploadResult.ContentType,
                            UploadedAt = DateTime.UtcNow
                        };
                        _context.ApplicationDocuments.Add(document);
                        
                        _logger.LogInformation($"Document uploaded for application {application.JobApplicationId}: {uploadResult.FileName}");
                    }
                    else
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.ErrorMessage}");
                    }
                }
                
                await _context.SaveChangesAsync();
                
                if (uploadErrors.Count > 0)
                {
                    TempData["WarningMessage"] = $"Application submitted, but some documents failed to upload: {string.Join(", ", uploadErrors)}";
                }
            }

            TempData["SuccessMessage"] = "Application submitted successfully!";
            return RedirectToAction("MyApplications");
        }

        // GET: JobApplications/ManageDocuments/5 (For Job Seekers to manage their application documents)
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> ManageDocuments(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var application = await _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                .Include(a => a.JobVacancy)
                .Include(a => a.ApplicationDocuments)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            // Verify ownership
            if (application.JobSeekerProfile.UserId != userId)
            {
                return Forbid();
            }

            // Can only manage documents for pending applications
            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] = "You can only manage documents for pending applications.";
                return RedirectToAction(nameof(MyApplications));
            }

            var viewModel = new JobApplicationUploadViewModel
            {
                JobApplicationId = application.JobApplicationId,
                ExistingDocuments = application.ApplicationDocuments
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new ApplicationDocumentViewModel
                    {
                        ApplicationDocumentId = d.ApplicationDocumentId,
                        JobApplicationId = d.JobApplicationId,
                        FilePath = d.FilePath,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        ContentType = d.ContentType,
                        UploadedAt = d.UploadedAt,
                        DownloadUrl = Url.Action("Download", "Files", new { id = d.ApplicationDocumentId })!
                    }).ToList()
            };

            ViewBag.JobTitle = application.JobVacancy.JobTitle;

            return View(viewModel);
        }

        // POST: JobApplications/Withdraw
        [HttpPost]
        [Authorize(Roles = "JobSeeker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = GetCurrentUserId();
            var application = await _context.JobApplications
                .Include(a => a.JobSeekerProfile)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.JobSeekerProfile.UserId != userId)
            {
                return Forbid();
            }

            application.Status = "Withdrawn";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application withdrawn successfully.";
            return RedirectToAction(nameof(MyApplications));
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

        private string GenerateResumeSnapshot(JobSeekerProfile profile)
        {
            var snapshot = new System.Text.StringBuilder();
            snapshot.AppendLine($"Name: {profile.FullName}");
            snapshot.AppendLine($"Phone: {profile.PhoneNumber}");
            snapshot.AppendLine($"Summary: {profile.Summary}");
            snapshot.AppendLine();

            snapshot.AppendLine("=== Education ===");
            foreach (var edu in profile.ResumeEducations.OrderByDescending(e => e.EndYear))
            {
                snapshot.AppendLine($"{edu.Qualification} in {edu.FieldOfStudy}");
                snapshot.AppendLine($"{edu.SchoolName} ({edu.StartYear} - {edu.EndYear})");
                snapshot.AppendLine();
            }

            snapshot.AppendLine("=== Experience ===");
            foreach (var exp in profile.ResumeExperiences.OrderByDescending(e => e.EndDate))
            {
                snapshot.AppendLine($"{exp.JobTitle} at {exp.CompanyName}");
                snapshot.AppendLine($"{exp.StartDate:MMM yyyy} - {(exp.EndDate.HasValue ? exp.EndDate.Value.ToString("MMM yyyy") : "Present")}");
                snapshot.AppendLine(exp.Description);
                snapshot.AppendLine();
            }

            snapshot.AppendLine("=== Skills ===");
            foreach (var skill in profile.ResumeSkills)
            {
                snapshot.AppendLine($"{skill.SkillName} - {skill.SkillLevel}");
            }

            return snapshot.ToString();
        }

        private async Task<string> SaveUploadedFile(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{uniqueFileName}";
        }

        private async Task<string> SavePdfToFileAsync(string fileName, byte[] pdfBytes)
        {
            var pdfFolder = Path.Combine(_environment.WebRootPath, "uploads", "pdfs");
            Directory.CreateDirectory(pdfFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(pdfFolder, uniqueFileName);

            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
            _logger.LogInformation("PDF saved to: {FilePath}", filePath);

            return $"/uploads/pdfs/{uniqueFileName}";
        }

        /// <summary>
        /// Builds the application timeline from status history, interview details, and offer details.
        /// Returns events in reverse chronological order (latest first).
        /// </summary>
        private List<ApplicationTimelineItemViewModel> BuildApplicationTimeline(JobApplication application)
        {
            var timeline = new List<ApplicationTimelineItemViewModel>();

            // Add "Application Submitted" event
            timeline.Add(new ApplicationTimelineItemViewModel
            {
                EventDate = application.ApplyDate,
                Status = "Applied",
                Reason = "Application submitted",
                EventType = "Applied"
            });

            // Get the first interview and offer details (if any)
            var interviewDetails = application.InterviewDetails?.FirstOrDefault();
            var offerDetails = application.OfferDetails?.FirstOrDefault();

            // Add status history events
            if (application.StatusHistory != null)
            {
                foreach (var history in application.StatusHistory)
                {
                    var timelineItem = new ApplicationTimelineItemViewModel
                    {
                        EventDate = history.CreatedAt,
                        Status = history.Status,
                        Reason = history.Reason,
                        EventType = "StatusUpdate"
                    };

                    // If this is an Interview status, attach interview details
                    if (history.Status == "Interview" && interviewDetails != null)
                    {
                        timelineItem.EventType = "Interview";
                        timelineItem.InterviewDate = interviewDetails.InterviewDate;
                        timelineItem.InterviewTime = interviewDetails.InterviewTime;
                        timelineItem.LocationOrLink = interviewDetails.LocationOrLink;
                        timelineItem.InterviewContactPerson = interviewDetails.ContactPerson;
                        timelineItem.InterviewContactPhone = interviewDetails.ContactPhone;
                        timelineItem.InterviewNotes = interviewDetails.Notes;
                        timelineItem.InterviewPdfPath = interviewDetails.PdfPath;
                    }

                    // If this is an Offered status, attach offer details
                    if (history.Status == "Offered" && offerDetails != null)
                    {
                        timelineItem.EventType = "Offer";
                        timelineItem.OfferJobTitle = offerDetails.JobTitle;
                        timelineItem.SalaryOffered = offerDetails.SalaryOffered;
                        timelineItem.StartDate = offerDetails.StartDate;
                        timelineItem.EmploymentType = offerDetails.EmploymentType;
                        timelineItem.OfferContactPerson = offerDetails.ContactPerson;
                        timelineItem.OfferContactPhone = offerDetails.ContactPhone;
                        timelineItem.OfferNotes = offerDetails.Notes;
                        timelineItem.OfferPdfPath = offerDetails.PdfPath;
                    }

                    timeline.Add(timelineItem);
                }
            }

            // Sort by date descending (latest first)
            return timeline.OrderByDescending(t => t.EventDate).ToList();
        }

        #endregion
    }
}

