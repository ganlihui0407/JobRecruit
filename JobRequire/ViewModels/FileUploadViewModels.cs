namespace JobRequire.ViewModels
{
    // =============================================
    // FILE UPLOAD VIEW MODELS
    // =============================================
    // Purpose: ViewModels for file upload operations.
    // Includes: Upload result, Validation result, Download result, Settings.
    // 
    // Design Decision: These are DTOs for file operations, not screen ViewModels.
    // Used by FileUploadService and controllers handling file uploads.
    // =============================================

    /// <summary>
    /// DTO representing the result of a file upload operation.
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public string? OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// DTO representing the result of file validation.
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// DTO containing file stream and metadata for downloads.
    /// </summary>
    public class FileDownloadResult
    {
        public Stream FileStream { get; set; } = null!;
        public string ContentType { get; set; } = "application/octet-stream";
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    /// <summary>
    /// Configuration settings for file uploads.
    /// Bound from appsettings or configured in Program.cs.
    /// </summary>
    public class FileUploadSettings
    {
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB default
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".docx", ".doc", ".jpg", ".jpeg", ".png" };
        public string[] AllowedMimeTypes { get; set; } = 
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/jpeg",
            "image/png"
        };
        public string UploadPath { get; set; } = "ProtectedFiles";
        public bool UseProtectedFolder { get; set; } = true;
    }

    /// <summary>
    /// ViewModel for application document
    /// </summary>
    public class ApplicationDocumentViewModel
    {
        public int ApplicationDocumentId { get; set; }
        public int JobApplicationId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;

        // Computed properties
        public string FileSizeDisplay
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            }
        }

        public string FileIcon
        {
            get
            {
                var ext = Path.GetExtension(FileName)?.ToLower();
                return ext switch
                {
                    ".pdf" => "bi-file-pdf text-danger",
                    ".doc" or ".docx" => "bi-file-word text-primary",
                    ".jpg" or ".jpeg" or ".png" => "bi-file-image text-success",
                    _ => "bi-file-earmark"
                };
            }
        }
    }

    /// <summary>
    /// Upload form for job application
    /// </summary>
    public class JobApplicationUploadViewModel
    {
        public int JobApplicationId { get; set; }
        public List<IFormFile> Documents { get; set; } = new();
        public List<ApplicationDocumentViewModel> ExistingDocuments { get; set; } = new();
    }
}

