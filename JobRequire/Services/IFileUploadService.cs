namespace JobRequire.Services
{
    public interface IFileUploadService
    {
        /// <summary>
        /// Upload a file and return the stored file path
        /// </summary>
        Task<FileUploadResult> UploadFileAsync(IFormFile file, int userId, string? subfolder = null);

        /// <summary>
        /// Delete a file from storage
        /// </summary>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Get file stream for download
        /// </summary>
        Task<FileStream?> GetFileStreamAsync(string filePath);

        /// <summary>
        /// Validate file before upload
        /// </summary>
        FileValidationResult ValidateFile(IFormFile file);

        /// <summary>
        /// Get allowed file extensions
        /// </summary>
        string[] GetAllowedExtensions();

        /// <summary>
        /// Get maximum file size in bytes
        /// </summary>
        long GetMaxFileSize();
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
