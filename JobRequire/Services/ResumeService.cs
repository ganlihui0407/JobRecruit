using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.ViewModels;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace JobRequire.Services
{
    public class ResumeService : IResumeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResumeService> _logger;
        private readonly IResumeCompletionService _completionService;
        private readonly IWebHostEnvironment _environment;

        public ResumeService(
            ApplicationDbContext context, 
            ILogger<ResumeService> logger,
            IResumeCompletionService completionService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _completionService = completionService;
            _environment = environment;
        }

        public async Task<ResumeSnapshotViewModel?> GetResumeSnapshotAsync(int jobSeekerProfileId)
        {
            var profile = await _context.JobSeekerProfiles
                .Include(p => p.User)
                .Include(p => p.ResumeEducations)
                .Include(p => p.ResumeExperiences)
                .Include(p => p.ResumeSkills)
                .FirstOrDefaultAsync(p => p.JobSeekerProfileId == jobSeekerProfileId);

            if (profile == null)
                return null;

            // Convert profile photo to Base64 for print/PDF rendering
            var profilePhotoBase64 = ConvertImageToBase64(profile.ProfilePhoto);

            return new ResumeSnapshotViewModel
            {
                FullName = profile.FullName,
                Email = profile.User?.Email,
                PhoneNumber = profile.PhoneNumber,
                Summary = profile.Summary,
                ProfilePhoto = profile.ProfilePhoto,
                ProfilePhotoBase64 = profilePhotoBase64,
                GeneratedAt = DateTime.UtcNow,
                Education = profile.ResumeEducations
                    .OrderByDescending(e => e.EndYear ?? 9999)
                    .ThenByDescending(e => e.StartYear)
                    .Select(e => new EducationSnapshot
                    {
                        SchoolName = e.SchoolName,
                        Qualification = e.Qualification,
                        FieldOfStudy = e.FieldOfStudy,
                        StartYear = e.StartYear,
                        EndYear = e.EndYear
                    }).ToList(),
                Experience = profile.ResumeExperiences
                    .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                    .ThenByDescending(e => e.StartDate)
                    .Select(e => new ExperienceSnapshot
                    {
                        CompanyName = e.CompanyName,
                        JobTitle = e.JobTitle,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Description = e.Description ?? string.Empty
                    }).ToList(),
                Skills = profile.ResumeSkills
                    .OrderBy(s => s.SkillName)
                    .Select(s => new SkillSnapshot
                    {
                        SkillName = s.SkillName,
                        SkillLevel = s.SkillLevel
                    }).ToList()
            };
        }

        /// <summary>
        /// Converts a profile photo from file path to Base64 data URI for print/PDF rendering.
        /// </summary>
        /// <param name="photoPath">Relative or absolute path to the photo</param>
        /// <returns>Base64 data URI (e.g., "data:image/jpeg;base64,...") or null if conversion fails</returns>
        private string? ConvertImageToBase64(string? photoPath)
        {
            if (string.IsNullOrEmpty(photoPath))
                return null;

            try
            {
                // Get absolute file path
                string absolutePath = GetAbsolutePhotoPath(photoPath);

                if (!File.Exists(absolutePath))
                {
                    _logger.LogWarning("Profile photo file not found: {Path}", absolutePath);
                    return null;
                }

                // Read file bytes
                byte[] imageBytes = File.ReadAllBytes(absolutePath);

                // Determine MIME type from extension
                string extension = Path.GetExtension(absolutePath).ToLowerInvariant();
                string mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    _ => "image/jpeg" // Default fallback
                };

                // Convert to Base64 data URI
                string base64 = Convert.ToBase64String(imageBytes);
                return $"data:{mimeType};base64,{base64}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert profile photo to Base64: {Path}", photoPath);
                return null;
            }
        }

        /// <summary>
        /// Converts a relative photo path to an absolute file system path.
        /// </summary>
        private string GetAbsolutePhotoPath(string photoPath)
        {
            // If it's already an absolute path, return it
            if (Path.IsPathRooted(photoPath))
                return photoPath;

            // If it starts with /, it's a web path - convert to file path
            if (photoPath.StartsWith("/"))
            {
                var relativePath = photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                return Path.Combine(_environment.WebRootPath, relativePath);
            }

            // Otherwise, assume it's relative to wwwroot
            return Path.Combine(_environment.WebRootPath, photoPath.Replace('/', Path.DirectorySeparatorChar));
        }

        public async Task<string> GenerateResumeSnapshotJsonAsync(int jobSeekerProfileId)
        {
            var snapshot = await GetResumeSnapshotAsync(jobSeekerProfileId);
            if (snapshot == null)
                return "{}";

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public async Task<string> GenerateResumeHtmlAsync(int jobSeekerProfileId)
        {
            var snapshot = await GetResumeSnapshotAsync(jobSeekerProfileId);
            if (snapshot == null)
                return "<p>Resume not found.</p>";

            // Generate HTML using the SAME structure as _ResumeTemplate.cshtml
            // This ensures visual consistency across Preview, Print-Ready, and PDF
            var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{System.Net.WebUtility.HtmlEncode(snapshot.FullName)} - Resume</title>
    <style>
        /* Unified Resume Styles - matches resume.css */
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background: #fff;
        }}
        .resume-wrapper {{ max-width: 800px; margin: 0 auto; background: #fff; }}
        .resume-card {{ background: #fff; }}
        .resume-body {{ padding: 2.5rem; }}
        
        /* Header */
        .resume-header {{
            text-align: center;
            margin-bottom: 1.5rem;
            padding-bottom: 1.5rem;
            border-bottom: 2px solid #3b82f6;
        }}
        .resume-photo {{
            width: 120px;
            height: 120px;
            border-radius: 50%;
            object-fit: cover;
            margin-bottom: 1rem;
            border: 3px solid #e5e7eb;
        }}
        .resume-name {{
            font-size: 1.75rem;
            font-weight: 700;
            color: #1e3a5f;
            margin-bottom: 0.5rem;
        }}
        .resume-contact {{
            display: flex;
            justify-content: center;
            flex-wrap: wrap;
            gap: 1.5rem;
            color: #6b7280;
            font-size: 0.95rem;
        }}
        .resume-contact-item {{
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }}
        
        /* Sections */
        .resume-section {{ margin-bottom: 1.5rem; }}
        .resume-section-title {{
            font-size: 1.1rem;
            font-weight: 600;
            color: #3b82f6;
            border-bottom: 1px solid #e5e7eb;
            padding-bottom: 0.5rem;
            margin-bottom: 1rem;
        }}
        .resume-summary {{
            color: #4b5563;
            line-height: 1.7;
            white-space: pre-wrap;
        }}
        
        /* Items */
        .resume-item {{
            margin-bottom: 1rem;
            padding-left: 1rem;
            border-left: 3px solid #3b82f6;
        }}
        .resume-item:last-child {{ margin-bottom: 0; }}
        .resume-item.education-item {{ border-left-color: #06b6d4; }}
        .resume-item-header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            flex-wrap: wrap;
            gap: 0.5rem;
            margin-bottom: 0.25rem;
        }}
        .resume-item-title {{
            font-weight: 600;
            color: #1f2937;
            margin-bottom: 0.125rem;
        }}
        .resume-item-subtitle {{ color: #6b7280; font-size: 0.95rem; }}
        .resume-item-date {{
            color: #9ca3af;
            font-size: 0.85rem;
            white-space: nowrap;
            background: #f3f4f6;
            padding: 0.25rem 0.75rem;
            border-radius: 4px;
        }}
        .resume-item-description {{
            color: #4b5563;
            font-size: 0.9rem;
            margin-top: 0.5rem;
            white-space: pre-wrap;
            line-height: 1.6;
        }}
        
        /* Skills */
        .resume-skills-grid {{ display: flex; flex-wrap: wrap; gap: 0.5rem; }}
        .resume-skill-tag {{
            display: inline-flex;
            align-items: center;
            padding: 0.375rem 0.75rem;
            border-radius: 20px;
            font-size: 0.875rem;
            font-weight: 500;
        }}
        .resume-skill-tag.expert {{ background: #dcfce7; color: #166534; border: 1px solid #bbf7d0; }}
        .resume-skill-tag.advanced {{ background: #dbeafe; color: #1e40af; border: 1px solid #bfdbfe; }}
        .resume-skill-tag.intermediate {{ background: #cffafe; color: #0e7490; border: 1px solid #a5f3fc; }}
        .resume-skill-tag.beginner {{ background: #f3f4f6; color: #4b5563; border: 1px solid #e5e7eb; }}
        .resume-skill-level {{ font-size: 0.75rem; opacity: 0.8; margin-left: 0.375rem; }}
        
        /* Footer */
        .resume-footer {{
            text-align: center;
            color: #9ca3af;
            font-size: 0.8rem;
            margin-top: 1.5rem;
            padding-top: 1rem;
            border-top: 1px solid #e5e7eb;
        }}
        
        /* Print styles */
        @media print {{
            body {{ background: #fff !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }}
            .resume-wrapper {{ max-width: 100%; margin: 0; padding: 0; }}
            .resume-body {{ padding: 0.5in; }}
            .resume-section {{ page-break-inside: avoid; }}
            .resume-item {{ page-break-inside: avoid; }}
            .resume-section-title {{ color: #3b82f6 !important; }}
            .resume-skill-tag {{ -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }}
            @page {{ margin: 0.5in; size: letter; }}
        }}
    </style>
</head>
<body>
    <div class=""resume-wrapper"">
        <div class=""resume-card"">
            <div class=""resume-body"">
                <!-- Header -->
                <div class=""resume-header"">
                    {(string.IsNullOrEmpty(snapshot.ProfilePhotoBase64) && string.IsNullOrEmpty(snapshot.ProfilePhoto) ? "" : $@"<img src=""{(string.IsNullOrEmpty(snapshot.ProfilePhotoBase64) ? System.Net.WebUtility.HtmlEncode(snapshot.ProfilePhoto) : snapshot.ProfilePhotoBase64)}"" alt=""{System.Net.WebUtility.HtmlEncode(snapshot.FullName)}"" class=""resume-photo"" />")}
                    <h1 class=""resume-name"">{System.Net.WebUtility.HtmlEncode(snapshot.FullName)}</h1>
                    <div class=""resume-contact"">
                        {(string.IsNullOrEmpty(snapshot.Email) ? "" : $@"<span class=""resume-contact-item"">📧 {System.Net.WebUtility.HtmlEncode(snapshot.Email)}</span>")}
                        {(string.IsNullOrEmpty(snapshot.PhoneNumber) ? "" : $@"<span class=""resume-contact-item"">📱 {System.Net.WebUtility.HtmlEncode(snapshot.PhoneNumber)}</span>")}
                    </div>
                </div>

                {(string.IsNullOrEmpty(snapshot.Summary) ? "" : $@"
                <!-- Professional Summary -->
                <section class=""resume-section"">
                    <h2 class=""resume-section-title"">📋 Professional Summary</h2>
                    <p class=""resume-summary"">{System.Net.WebUtility.HtmlEncode(snapshot.Summary)}</p>
                </section>")}

                {(snapshot.Experience.Count == 0 ? "" : $@"
                <!-- Work Experience -->
                <section class=""resume-section"">
                    <h2 class=""resume-section-title"">💼 Work Experience</h2>
                    {string.Join("", snapshot.Experience.Select(exp => $@"
                    <div class=""resume-item"">
                        <div class=""resume-item-header"">
                            <div>
                                <div class=""resume-item-title"">{System.Net.WebUtility.HtmlEncode(exp.JobTitle)}</div>
                                <div class=""resume-item-subtitle"">{System.Net.WebUtility.HtmlEncode(exp.CompanyName)}</div>
                            </div>
                            <span class=""resume-item-date"">{(exp.StartDate.HasValue ? exp.StartDate.Value.ToString("MMM yyyy") : "N/A")} - {(exp.EndDate.HasValue ? exp.EndDate.Value.ToString("MMM yyyy") : "Present")}</span>
                        </div>
                        {(string.IsNullOrEmpty(exp.Description) ? "" : $@"<p class=""resume-item-description"">{System.Net.WebUtility.HtmlEncode(exp.Description)}</p>")}
                    </div>"))}
                </section>")}

                {(snapshot.Education.Count == 0 ? "" : $@"
                <!-- Education -->
                <section class=""resume-section"">
                    <h2 class=""resume-section-title"">🎓 Education</h2>
                    {string.Join("", snapshot.Education.Select(edu => $@"
                    <div class=""resume-item education-item"">
                        <div class=""resume-item-header"">
                            <div>
                                <div class=""resume-item-title"">{System.Net.WebUtility.HtmlEncode(edu.Qualification)}{(string.IsNullOrEmpty(edu.FieldOfStudy) ? "" : $" in {System.Net.WebUtility.HtmlEncode(edu.FieldOfStudy)}")}</div>
                                <div class=""resume-item-subtitle"">{System.Net.WebUtility.HtmlEncode(edu.SchoolName)}</div>
                            </div>
                            <span class=""resume-item-date"">{edu.StartYear} - {(edu.EndYear.HasValue ? edu.EndYear.Value.ToString() : "Present")}</span>
                        </div>
                    </div>"))}
                </section>")}

                {(snapshot.Skills.Count == 0 ? "" : $@"
                <!-- Skills -->
                <section class=""resume-section"">
                    <h2 class=""resume-section-title"">⭐ Skills</h2>
                    <div class=""resume-skills-grid"">
                        {string.Join("", snapshot.Skills.Select(skill => {
                            var levelClass = (skill.SkillLevel?.ToLower()) switch
                            {
                                "expert" => "expert",
                                "advanced" => "advanced",
                                "intermediate" => "intermediate",
                                _ => "beginner"
                            };
                            return $@"<span class=""resume-skill-tag {levelClass}"">{System.Net.WebUtility.HtmlEncode(skill.SkillName)}<span class=""resume-skill-level"">({System.Net.WebUtility.HtmlEncode(skill.SkillLevel)})</span></span>";
                        }))}
                    </div>
                </section>")}

                <!-- Footer -->
                <div class=""resume-footer"">
                    <p>Generated on {snapshot.GeneratedAt:MMMM dd, yyyy}</p>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        public Task<int> CalculateCompletionPercentageAsync(int jobSeekerProfileId)
        {
            // Delegate to the shared completion service for consistent calculation
            return _completionService.CalculateCompletionAsync(jobSeekerProfileId);
        }

        public async Task<bool> UpdateResumeStatusAsync(int jobSeekerProfileId, string status)
        {
            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.JobSeekerProfileId == jobSeekerProfileId);

            if (profile == null)
                return false;

            // ResumeStatus and ResumeLastUpdated properties removed from model
            // Status tracking can be handled via ActivityLogs if needed
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

