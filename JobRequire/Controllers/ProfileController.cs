using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Services;
using JobRequire.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace JobRequire.Controllers
{
    /// <summary>
    /// Controller for public profile access.
    /// All actions are accessible without authentication.
    /// </summary>
    [AllowAnonymous]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IResumeService _resumeService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            ApplicationDbContext context,
            IResumeService resumeService,
            ILogger<ProfileController> logger)
        {
            _context = context;
            _resumeService = resumeService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Profile/Public/{identifier}
        /// Public profile page accessible without login.
        /// Shows jobseeker profile and resume in read-only mode.
        /// </summary>
        [HttpGet("Profile/Public/{identifier}")]
        public async Task<IActionResult> PublicProfile(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                _logger.LogWarning("PublicProfile accessed with empty identifier");
                return NotFound();
            }

            // Find the profile by decoding the identifier
            var profileId = DecodePublicIdentifier(identifier);
            if (profileId == null)
            {
                _logger.LogWarning("PublicProfile: Invalid identifier format: {Identifier}", identifier);
                return NotFound();
            }

            var profile = await _context.JobSeekerProfiles
                .Include(j => j.User)
                .Include(j => j.ResumeEducations)
                .Include(j => j.ResumeExperiences)
                .Include(j => j.ResumeSkills)
                .FirstOrDefaultAsync(j => j.JobSeekerProfileId == profileId.Value);

            if (profile == null)
            {
                _logger.LogWarning("PublicProfile: Profile not found for ID: {ProfileId}", profileId.Value);
                return NotFound();
            }

            _logger.LogInformation("PublicProfile accessed for: {FullName} (ID: {ProfileId})", 
                profile.FullName, profile.JobSeekerProfileId);

            // Get the resume snapshot using the existing service
            var resumeSnapshot = await _resumeService.GetResumeSnapshotAsync(profile.JobSeekerProfileId);

            var viewModel = new PublicProfileViewModel
            {
                PublicIdentifier = identifier,
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Summary = profile.Summary,
                ProfilePhoto = profile.ProfilePhoto,
                Resume = resumeSnapshot,
                TotalExperience = profile.ResumeExperiences?.Count ?? 0,
                TotalEducation = profile.ResumeEducations?.Count ?? 0,
                TotalSkills = profile.ResumeSkills?.Count ?? 0,
                GeneratedAt = DateTime.UtcNow
            };

            return View(viewModel);
        }

        /// <summary>
        /// Generate a public identifier from profile ID.
        /// Uses a simple encoding that is stable and not easily guessable.
        /// This is a static method so it can be called from other controllers.
        /// </summary>
        public static string GeneratePublicIdentifier(int profileId)
        {
            // Create a stable hash-like identifier
            // Combine profile ID with a salt for security
            var data = $"JobRecruit_Public_{profileId}_Profile";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            // Take first 8 bytes and convert to URL-safe base64
            var shortHash = Convert.ToBase64String(hashBytes, 0, 8)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            // Prepend encoded profile ID for decoding
            var encodedId = Convert.ToBase64String(BitConverter.GetBytes(profileId))
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            return $"{encodedId}-{shortHash}";
        }

        /// <summary>
        /// Decode the public identifier to get the profile ID.
        /// </summary>
        public static int? DecodePublicIdentifier(string identifier)
        {
            try
            {
                // Split the identifier - only split on first dash to handle hash containing dashes
                var dashIndex = identifier.IndexOf('-');
                if (dashIndex < 0) return null;

                var encodedIdPart = identifier.Substring(0, dashIndex);
                var hashPart = identifier.Substring(dashIndex + 1);

                // Decode the profile ID from the first part
                var encodedId = encodedIdPart
                    .Replace("-", "+")
                    .Replace("_", "/");
                // Add padding if needed
                while (encodedId.Length % 4 != 0)
                    encodedId += "=";

                var idBytes = Convert.FromBase64String(encodedId);
                var profileId = BitConverter.ToInt32(idBytes, 0);

                // Verify the hash matches
                var expectedIdentifier = GeneratePublicIdentifier(profileId);
                if (expectedIdentifier == identifier)
                {
                    return profileId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}



