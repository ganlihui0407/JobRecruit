using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Email sender that writes emails to files and/or ActivityLogs for demo purposes
    /// </summary>
    public class FileEmailSender : IEmailSender
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileEmailSender> _logger;
        private readonly IConfiguration _configuration;

        // Email storage path
        private readonly string _emailsPath;

        public FileEmailSender(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment environment,
            ILogger<FileEmailSender> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
            _configuration = configuration;

            // Determine email storage path
            var emailsFolder = _configuration.GetValue<string>("EmailStorage:Path", "App_Data/Emails");
            if (Path.IsPathRooted(emailsFolder))
            {
                _emailsPath = emailsFolder;
            }
            else
            {
                _emailsPath = Path.Combine(_environment.ContentRootPath, emailsFolder);
            }

            // Ensure directory exists
            if (!Directory.Exists(_emailsPath))
            {
                Directory.CreateDirectory(_emailsPath);
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Log to console
            _logger.LogInformation("========== EMAIL SENT ==========");
            _logger.LogInformation($"To: {toEmail}");
            _logger.LogInformation($"Subject: {subject}");
            _logger.LogInformation($"Body: {body}");
            _logger.LogInformation("================================");

            // Save to file
            await SaveEmailToFileAsync(toEmail, subject, body);

            // Log to ActivityLogs (if user exists)
            await LogEmailToActivityLogsAsync(toEmail, subject, body);
        }

        public async Task SendEmailActivationLinkAsync(string toEmail, string userName, string activationLink)
        {
            var subject = "Activate Your JobRecruit Account";
            var body = $@"
Hello {userName},

Thank you for registering with JobRecruit!

Please click the link below to activate your account:
{activationLink}

IMPORTANT: This link will expire in 2 minutes.

If you did not create an account, please ignore this email.

Best regards,
JobRecruit Team
";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailVerificationOtpAsync(string toEmail, string userName, string otpCode)
        {
            var subject = "Verify your JobRecruit account";
            var body = $@"
Hello {userName},

Your verification code is: {otpCode}

This code will expire in 10 minutes.

If you did not create an account, please ignore this email.

Best regards,
JobRecruit Team
";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailVerificationAsync(string toEmail, string userName, string verificationLink)
        {
            var subject = "Verify Your Email - JobRecruit";
            var body = $@"
Hello {userName},

Thank you for registering with JobRecruit!

Please click the link below to verify your email address:
{verificationLink}

This link will expire in 24 hours.

If you did not create an account, please ignore this email.

Best regards,
JobRecruit Team
";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetAsync(string toEmail, string userName, string resetLink)
        {
            var subject = "Reset your JobRecruit password";
            var body = $@"
Hello {userName},

We received a request to reset your password.

Please click the link below to reset your password:
{resetLink}

This link will expire in 1 hour.

If you did not request a password reset, please ignore this email.

Best regards,
JobRecruit Team
";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetOtpAsync(string toEmail, string userName, string otpCode)
        {
            var subject = "Reset your JobRecruit password";
            var body = $@"
Hello {userName},

We received a request to reset your password.

Your verification code is: {otpCode}

This code will expire in 10 minutes.

If you did not request a password reset, please ignore this email.

Best regards,
JobRecruit Team
";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendJobApplicationNotificationAsync(string employerEmail, string employerName, string jobTitle, string applicantName, string dashboardLink)
        {
            var subject = "New Job Application Received";
            var body = $@"
Hello {employerName},

Great news! You have received a new application for your job posting.

Position: {jobTitle}
Applicant: {applicantName}

View the application at: {dashboardLink}

Best regards,
JobRecruit Team
";
            await SendEmailAsync(employerEmail, subject, body);
        }

        public async Task SendApplicationStatusUpdateAsync(string jobSeekerEmail, string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink)
        {
            var subject = $"Application Status Update - {newStatus}";
            var body = $@"
Hello {jobSeekerName},

Your application status has been updated.

Position: {jobTitle}
Company: {companyName}
New Status: {newStatus}

View your applications at: {applicationsLink}

Best regards,
JobRecruit Team
";
            await SendEmailAsync(jobSeekerEmail, subject, body);
        }

        public async Task SendApplicationStatusUpdateWithAttachmentAsync(string jobSeekerEmail, string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink, byte[] pdfAttachment, string attachmentFileName)
        {
            var subject = $"Application Status Update - {newStatus}";
            var body = $@"
Hello {jobSeekerName},

Your application status has been updated.

Position: {jobTitle}
Company: {companyName}
New Status: {newStatus}

Attachment: {attachmentFileName} ({pdfAttachment?.Length ?? 0} bytes)

View your applications at: {applicationsLink}

Best regards,
JobRecruit Team
";
            await SendEmailAsync(jobSeekerEmail, subject, body);

            // Save attachment to file
            if (pdfAttachment != null && pdfAttachment.Length > 0)
            {
                await SaveAttachmentToFileAsync(attachmentFileName, pdfAttachment);
            }
        }

        private async Task SaveAttachmentToFileAsync(string fileName, byte[] content)
        {
            try
            {
                var dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var attachmentsPath = Path.Combine(_emailsPath, dateFolder, "attachments");
                Directory.CreateDirectory(attachmentsPath);

                var filePath = Path.Combine(attachmentsPath, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{fileName}");
                await File.WriteAllBytesAsync(filePath, content);
                _logger.LogInformation($"Attachment saved to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save attachment to file");
            }
        }

        private async Task SaveEmailToFileAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Create subfolder by date
                var dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var datePath = Path.Combine(_emailsPath, dateFolder);
                Directory.CreateDirectory(datePath);

                // Create filename with timestamp
                var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.txt";
                var filePath = Path.Combine(datePath, fileName);

                var content = $@"To: {toEmail}
Subject: {subject}
Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
From: noreply@jobrecruitment.com

{body}
";
                await File.WriteAllTextAsync(filePath, content);
                _logger.LogInformation($"Email saved to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save email to file");
            }
        }

        private async Task LogEmailToActivityLogsAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Try to find user by email
                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == toEmail);

                if (user != null)
                {
                    var activityLog = new ActivityLog
                    {
                        UserId = user.UserId,
                        Action = "EmailSent",
                        Timestamp = DateTime.UtcNow,
                        Details = $"Email sent: {subject}"
                    };

                    context.ActivityLogs.Add(activityLog);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log email to ActivityLogs");
            }
        }
    }
}

