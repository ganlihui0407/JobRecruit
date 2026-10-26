namespace JobRequire.ViewModels
{
    // =============================================
    // ERROR VIEW MODEL
    // =============================================
    // Purpose: ViewModel for displaying error pages.
    // =============================================

    /// <summary>
    /// ViewModel for the error page.
    /// Contains request ID for debugging and optional error message.
    /// </summary>
    /// <remarks>
    /// Usage: /Home/Error
    /// RequestId is shown in development mode for debugging.
    /// </remarks>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string? ErrorMessage { get; set; }

        public int? StatusCode { get; set; }
    }
}

