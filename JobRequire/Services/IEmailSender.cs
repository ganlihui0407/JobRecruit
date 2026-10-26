namespace JobRequire.Services
{
    public interface IEmailSender
    {
        /// <summary>
        /// Send a generic email
        /// </summary>
        Task SendEmailAsync(string toEmail, string subject, string body);
        
        /// <summary>
        /// Send email activation link with 2-minute expiry
        /// </summary>
        Task SendEmailActivationLinkAsync(string toEmail, string userName, string activationLink);
        
        /// <summary>
        /// Send email verification OTP code (deprecated - kept for backward compatibility)
        /// </summary>
        Task SendEmailVerificationOtpAsync(string toEmail, string userName, string otpCode);
        
        /// <summary>
        /// Send email verification link (legacy support)
        /// </summary>
        Task SendEmailVerificationAsync(string toEmail, string userName, string verificationLink);
        
        /// <summary>
        /// Send password reset email with secure link (legacy support)
        /// </summary>
        Task SendPasswordResetAsync(string toEmail, string userName, string resetLink);
        
        /// <summary>
        /// Send password reset OTP code
        /// </summary>
        Task SendPasswordResetOtpAsync(string toEmail, string userName, string otpCode);
        
        /// <summary>
        /// Send notification to employer when a jobseeker applies
        /// </summary>
        Task SendJobApplicationNotificationAsync(string employerEmail, string employerName, string jobTitle, string applicantName, string dashboardLink);
        
        /// <summary>
        /// Send notification to jobseeker when application status changes
        /// </summary>
        Task SendApplicationStatusUpdateAsync(string jobSeekerEmail, string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink);

        /// <summary>
        /// Send notification to jobseeker when application status changes with PDF attachment
        /// </summary>
        Task SendApplicationStatusUpdateWithAttachmentAsync(string jobSeekerEmail, string jobSeekerName, string jobTitle, string companyName, string newStatus, string applicationsLink, byte[] pdfAttachment, string attachmentFileName);
    }
}


