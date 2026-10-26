using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.Services;
using System.Security.Claims;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for secure file download with authorization checks
    /// </summary>
    [Authorize]
    public class FilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            ILogger<FilesController> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        /// <summary>
        /// Download an application document
        /// Only accessible by:
        /// - The job seeker who uploaded it
        /// - The employer who posted the job
        /// - Admins
        /// </summary>
        [HttpGet]
        [Route("Files/Download/{id}")]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.ApplicationDocuments
                .Include(d => d.JobApplication)
                    .ThenInclude(a => a.JobVacancy)
                        .ThenInclude(v => v.EmployerProfile)
                            .ThenInclude(e => e.User)
                .Include(d => d.JobApplication)
                    .ThenInclude(a => a.JobSeekerProfile)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(d => d.ApplicationDocumentId == id);

            if (document == null)
            {
                return NotFound("Document not found.");
            }

            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Authorization check
            bool isAuthorized = false;

            if (userRole == "Admin")
            {
                isAuthorized = true;
            }
            else if (userRole == "JobSeeker")
            {
                // Job seeker can download their own documents
                isAuthorized = document.JobApplication.JobSeekerProfile.UserId == userId;
            }
            else if (userRole == "Employer")
            {
                // Employer can download documents for their job postings
                isAuthorized = document.JobApplication.JobVacancy.EmployerProfile.UserId == userId;
            }

            if (!isAuthorized)
            {
                _logger.LogWarning($"Unauthorized download attempt: User {userId} tried to download document {id}");
                return Forbid("You do not have permission to download this file.");
            }

            // Get file stream
            var fileStream = await _fileUploadService.GetFileStreamAsync(document.FilePath);
            if (fileStream == null)
            {
                return NotFound("File not found on server.");
            }

            // Determine content type
            var contentType = !string.IsNullOrEmpty(document.ContentType)
                ? document.ContentType
                : FileUploadService.GetContentType(Path.GetExtension(document.FileName));

            // Log download activity
            var activityLog = new ActivityLog
            {
                UserId = userId!.Value,
                Action = "FileDownloaded",
                Timestamp = DateTime.UtcNow,
                Details = $"Downloaded document: {document.FileName} (Application: {document.JobApplicationId})"
            };
            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();

            // Return file
            return File(fileStream, contentType, document.FileName);
        }

        /// <summary>
        /// Delete an application document
        /// Only the job seeker who uploaded it can delete it
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "JobSeeker,Admin")]
        [Route("Files/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.ApplicationDocuments
                .Include(d => d.JobApplication)
                    .ThenInclude(a => a.JobSeekerProfile)
                .FirstOrDefaultAsync(d => d.ApplicationDocumentId == id);

            if (document == null)
            {
                return Json(new { success = false, message = "Document not found." });
            }

            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Authorization check
            bool isAuthorized = userRole == "Admin" ||
                               document.JobApplication.JobSeekerProfile.UserId == userId;

            if (!isAuthorized)
            {
                return Json(new { success = false, message = "You do not have permission to delete this file." });
            }

            try
            {
                // Delete file from storage
                await _fileUploadService.DeleteFileAsync(document.FilePath);

                // Delete database record
                _context.ApplicationDocuments.Remove(document);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "File deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting document {id}");
                return Json(new { success = false, message = "An error occurred while deleting the file." });
            }
        }

        /// <summary>
        /// Get file information (for AJAX calls)
        /// </summary>
        [HttpGet]
        [Route("Files/Info/{id}")]
        public async Task<IActionResult> GetFileInfo(int id)
        {
            var document = await _context.ApplicationDocuments
                .Include(d => d.JobApplication)
                    .ThenInclude(a => a.JobVacancy)
                        .ThenInclude(v => v.EmployerProfile)
                .Include(d => d.JobApplication)
                    .ThenInclude(a => a.JobSeekerProfile)
                .FirstOrDefaultAsync(d => d.ApplicationDocumentId == id);

            if (document == null)
            {
                return Json(new { success = false, message = "Document not found." });
            }

            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Authorization check (same as download)
            bool isAuthorized = userRole == "Admin" ||
                               (userRole == "JobSeeker" && document.JobApplication.JobSeekerProfile.UserId == userId) ||
                               (userRole == "Employer" && document.JobApplication.JobVacancy.EmployerProfile.UserId == userId);

            if (!isAuthorized)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            return Json(new
            {
                success = true,
                document = new
                {
                    id = document.ApplicationDocumentId,
                    fileName = document.FileName,
                    fileSize = document.FileSize,
                    contentType = document.ContentType,
                    uploadedAt = document.UploadedAt,
                    downloadUrl = Url.Action("Download", "Files", new { id = document.ApplicationDocumentId })
                }
            });
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
}
