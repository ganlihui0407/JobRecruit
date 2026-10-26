namespace JobRequire.Configuration
{
    /// <summary>
    /// Email configuration settings for SMTP email sending.
    /// Bound from appsettings.json "EmailSettings" section.
    /// </summary>
    /// <remarks>
    /// Design Decision: Configuration class moved from ViewModels to Configuration folder.
    /// This is not a ViewModel - it's a POCO for configuration binding.
    /// </remarks>
    public class EmailSettings
    {
        /// <summary>
        /// SMTP server hostname (default: Gmail SMTP).
        /// </summary>
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        /// <summary>
        /// SMTP server port (default: 587 for TLS).
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Whether to use SSL/TLS encryption.
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// Email address to send from.
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the sender.
        /// </summary>
        public string SenderName { get; set; } = "JobRecruit";

        /// <summary>
        /// App-specific password for SMTP authentication.
        /// For Gmail, generate at: https://myaccount.google.com/apppasswords
        /// </summary>
        public string AppPassword { get; set; } = string.Empty;

        /// <summary>
        /// Master switch to enable/disable email sending.
        /// Useful for development/testing environments.
        /// </summary>
        public bool EnableEmailSending { get; set; } = true;
    }
}
