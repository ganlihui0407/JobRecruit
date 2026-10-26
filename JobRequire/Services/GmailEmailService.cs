using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using JobRequire.Configuration;

namespace JobRequire.Services
{
    /// <summary>
    /// Gmail SMTP Email Service for sending emails via Gmail
    /// Uses App Password for authentication (NOT raw Gmail password)
    /// </summary>
    public class GmailEmailService : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<GmailEmailService> _logger;

        public GmailEmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<GmailEmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Send a generic email via Gmail SMTP
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (!_emailSettings.EnableEmailSending)
            {
                _logger.LogInformation("Email sending is disabled. Would have sent to: {Email}, Subject: {Subject}", toEmail, subject);
                return;
            }

            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // 30 seconds timeout
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to: {Email}, Subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - email failures should not crash main business logic
                _logger.LogError(ex, "Failed to send email to: {Email}, Subject: {Subject}", toEmail, subject);
            }
        }

        /// <summary>
        /// Send email activation link with 2-minute expiry
        /// </summary>
        public async Task SendEmailActivationLinkAsync(string toEmail, string userName, string activationLink)
        {
            var subject = "Activate Your JobRecruit Account";
            var body = GetEmailActivationLinkTemplate(userName, activationLink);
            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Send email verification OTP code (deprecated - kept for backward compatibility)
        /// </summary>
        public async Task SendEmailVerificationOtpAsync(string toEmail, string userName, string otpCode)
        {
            var subject = "Verify your JobRecruit account";
            var body = GetEmailVerificationOtpTemplate(userName, otpCode);
            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Send email verification link (legacy support)
        /// </summary>
        public async Task SendEmailVerificationAsync(string toEmail, string userName, string verificationLink)
        {
            var subject = "Verify Your Email - JobRecruit";
            var body = GetEmailVerificationLinkTemplate(userName, verificationLink);
            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Send password reset email with secure link (legacy support)
        /// </summary>
        public async Task SendPasswordResetAsync(string toEmail, string userName, string resetLink)
        {
            var subject = "Reset your JobRecruit password";
            var body = GetPasswordResetLinkTemplate(userName, resetLink);
            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Send password reset OTP code
        /// </summary>
        public async Task SendPasswordResetOtpAsync(string toEmail, string userName, string otpCode)
        {
            var subject = "Reset your JobRecruit password";
            var body = GetPasswordResetOtpTemplate(userName, otpCode);
            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Send notification to employer when a jobseeker applies
        /// </summary>
        public async Task SendJobApplicationNotificationAsync(string employerEmail, string employerName, string jobTitle, string applicantName, string dashboardLink)
        {
            var subject = "New Job Application Received";
            var body = GetJobApplicationNotificationTemplate(employerName, jobTitle, applicantName, dashboardLink);
            await SendEmailAsync(employerEmail, subject, body);
        }

        /// <summary>
        /// Send notification to jobseeker when application status changes
        /// </summary>
        public async Task SendApplicationStatusUpdateAsync(string jobSeekerEmail, string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink)
        {
            var subject = GetStatusUpdateSubject(newStatus);
            var body = GetApplicationStatusUpdateTemplate(jobSeekerName, jobTitle, companyName, newStatus, applicationsLink);
            await SendEmailAsync(jobSeekerEmail, subject, body);
        }

        /// <summary>
        /// Send notification to jobseeker when application status changes with PDF attachment
        /// </summary>
        public async Task SendApplicationStatusUpdateWithAttachmentAsync(string jobSeekerEmail, string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink, byte[] pdfAttachment, string attachmentFileName)
        {
            if (!_emailSettings.EnableEmailSending)
            {
                _logger.LogInformation("Email sending is disabled. Would have sent to: {Email} with attachment: {FileName}", jobSeekerEmail, attachmentFileName);
                return;
            }

            try
            {
                var subject = GetStatusUpdateSubject(newStatus);
                var body = GetApplicationStatusUpdateWithAttachmentTemplate(jobSeekerName, jobTitle, companyName, newStatus, applicationsLink, attachmentFileName);

                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(jobSeekerEmail);

                // Add PDF attachment
                if (pdfAttachment != null && pdfAttachment.Length > 0)
                {
                    var stream = new MemoryStream(pdfAttachment);
                    var attachment = new Attachment(stream, attachmentFileName, "application/pdf");
                    mailMessage.Attachments.Add(attachment);
                }

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email with attachment sent successfully to: {Email}, Attachment: {FileName}", jobSeekerEmail, attachmentFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with attachment to: {Email}", jobSeekerEmail);
            }
        }

        #region Email Templates

        private static string GetEmailVerificationOtpTemplate(string userName, string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Verify Your Email Address</h2>
        <p>Hello <strong>{userName}</strong>,</p>
        <p>Thank you for registering with JobRecruit! To complete your registration, please enter the following verification code:</p>
        <div style='background: #f5f5f5; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px;'>
            <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #667eea;'>{otpCode}</span>
        </div>
        <p style='color: #666;'><strong>This code will expire in 10 minutes.</strong></p>
        <p>If you did not create an account with JobRecruit, please ignore this email.</p>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        private static string GetEmailActivationLinkTemplate(string userName, string activationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Activate Your Account</h2>
        <p>Hello <strong>{userName}</strong>,</p>
        <p>Thank you for registering with JobRecruit! Please click the button below to activate your account:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{activationLink}' style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 16px 32px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block; font-size: 16px;'>Activate My Account</a>
        </div>
        <div style='background: #fff3cd; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
            <p style='margin: 0; color: #856404;'><strong>⚠️ Important:</strong> This activation link will expire in <strong>2 minutes</strong>.</p>
            <p style='margin: 5px 0 0 0; color: #856404; font-size: 14px;'>If the link expires, you can request a new one from the login page.</p>
        </div>
        <p>If you did not create an account with JobRecruit, please ignore this email.</p>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        private static string GetEmailVerificationLinkTemplate(string userName, string verificationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Verify Your Email Address</h2>
        <p>Hello <strong>{userName}</strong>,</p>
        <p>Thank you for registering with JobRecruit! Please click the button below to verify your email address:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{verificationLink}' style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Verify Email</a>
        </div>
        <p style='color: #666;'>This link will expire in 24 hours.</p>
        <p>If you did not create an account with JobRecruit, please ignore this email.</p>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        private static string GetPasswordResetLinkTemplate(string userName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Reset Your Password</h2>
        <p>Hello <strong>{userName}</strong>,</p>
        <p>We received a request to reset your password. Click the button below to create a new password:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{resetLink}' style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Reset Password</a>
        </div>
        <p style='color: #666;'><strong>This link will expire in 1 hour.</strong></p>
        <p>If you did not request a password reset, please ignore this email. Your password will remain unchanged.</p>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        private static string GetPasswordResetOtpTemplate(string userName, string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Reset Your Password</h2>
        <p>Hello <strong>{userName}</strong>,</p>
        <p>We received a request to reset your password. Please enter the following verification code to continue:</p>
        <div style='background: #f5f5f5; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px;'>
            <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #667eea;'>{otpCode}</span>
        </div>
        <p style='color: #666;'><strong>This code will expire in 10 minutes.</strong></p>
        <p>If you did not request a password reset, please ignore this email. Your password will remain unchanged.</p>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        private static string GetJobApplicationNotificationTemplate(string employerName, string jobTitle, string applicantName, string dashboardLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>New Job Application Received</h2>
        <p>Hello <strong>{employerName}</strong>,</p>
        <p>Great news! You have received a new application for your job posting.</p>
        <div style='background: #f5f5f5; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 5px 0;'><strong>Position:</strong> {jobTitle}</p>
            <p style='margin: 5px 0;'><strong>Applicant:</strong> {applicantName}</p>
        </div>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{dashboardLink}' style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>View Application</a>
        </div>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        private static string GetStatusUpdateSubject(string status)
        {
            return status.ToLower() switch
            {
                "interview" => "Interview Invitation - JobRecruit",
                "shortlisted" => "You've Been Shortlisted! - JobRecruit",
                "hired" => "Congratulations! You've Been Hired - JobRecruit",
                "rejected" => "Application Update - JobRecruit",
                _ => "Application Status Update - JobRecruit"
            };
        }

        private static string GetApplicationStatusUpdateTemplate(string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink)
        {
            var statusColor = newStatus.ToLower() switch
            {
                "interview" or "shortlisted" or "hired" => "#28a745",
                "rejected" => "#dc3545",
                _ => "#667eea"
            };

            var statusMessage = newStatus.ToLower() switch
            {
                "interview" => "Congratulations! You have been selected for an interview.",
                "shortlisted" => "Great news! Your application has been shortlisted.",
                "hired" => "Congratulations! You have been hired for this position!",
                "rejected" => "We regret to inform you that your application was not selected for this position.",
                "pending" => "Your application is being reviewed.",
                _ => $"Your application status has been updated to: {newStatus}"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Application Status Update</h2>
        <p>Hello <strong>{jobSeekerName}</strong>,</p>
        <p>{statusMessage}</p>
        <div style='background: #f5f5f5; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 5px 0;'><strong>Position:</strong> {jobTitle}</p>
            <p style='margin: 5px 0;'><strong>Company:</strong> {companyName}</p>
            <p style='margin: 5px 0;'><strong>Status:</strong> <span style='color: {statusColor}; font-weight: bold;'>{newStatus}</span></p>
        </div>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{applicationsLink}' style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>View My Applications</a>
        </div>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        private static string GetApplicationStatusUpdateWithAttachmentTemplate(string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink, string attachmentFileName)
        {
            var statusColor = newStatus.ToLower() switch
            {
                "interview" or "offered" => "#28a745",
                _ => "#667eea"
            };

            var (statusMessage, attachmentNote) = newStatus.ToLower() switch
            {
                "interview" => ("Congratulations! You have been selected for an interview.", "📎 Please find the <strong>Interview Invitation</strong> attached to this email with all the details."),
                "offered" => ("Congratulations! We are pleased to extend a job offer to you!", "📎 Please find the <strong>Offer Letter</strong> attached to this email with all the details."),
                _ => ($"Your application status has been updated to: {newStatus}", "")
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>JobRecruit</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Application Status Update</h2>
        <p>Hello <strong>{jobSeekerName}</strong>,</p>
        <p>{statusMessage}</p>
        <div style='background: #f5f5f5; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 5px 0;'><strong>Position:</strong> {jobTitle}</p>
            <p style='margin: 5px 0;'><strong>Company:</strong> {companyName}</p>
            <p style='margin: 5px 0;'><strong>Status:</strong> <span style='color: {statusColor}; font-weight: bold;'>{newStatus}</span></p>
        </div>
        {(string.IsNullOrEmpty(attachmentNote) ? "" : $@"
        <div style='background: #e8f5e9; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745;'>
            <p style='margin: 0;'>{attachmentNote}</p>
            <p style='margin: 5px 0 0 0; font-size: 12px; color: #666;'>Attachment: {attachmentFileName}</p>
        </div>")}
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{applicationsLink}' style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>View My Applications</a>
        </div>
        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>This is an automated message from JobRecruit. Please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        #endregion
    }
}









