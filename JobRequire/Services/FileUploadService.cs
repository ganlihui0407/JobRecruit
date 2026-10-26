using System.Security.Cryptography;

namespace JobRequire.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IConfiguration _configuration;

        // Allowed file extensions
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".jpg", ".jpeg", ".png" };

        // Allowed MIME types
        private readonly Dictionary<string, string[]> _allowedMimeTypes = new()
        {
            { ".pdf", new[] { "application/pdf" } },
            { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
            { ".jpg", new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png", new[] { "image/png" } }
        };

        // Default max file size: 10MB
        private readonly long _maxFileSize;

        // Protected files folder (outside wwwroot)
        private readonly string _protectedFilesPath;

        public FileUploadService(
            IWebHostEnvironment environment,
            ILogger<FileUploadService> logger,
            IConfiguration configuration)
        {
            _environment = environment;
            _logger = logger;
            _configuration = configuration;

            // Get max file size from config or default to 10MB
            _maxFileSize = configuration.GetValue<long>("FileUpload:MaxFileSizeBytes", 10 * 1024 * 1024);

            // Set protected files path (outside wwwroot)
            var contentRoot = _environment.ContentRootPath;
            _protectedFilesPath = Path.Combine(contentRoot, "ProtectedFiles");

            // Ensure directory exists
            if (!Directory.Exists(_protectedFilesPath))
            {
                Directory.CreateDirectory(_protectedFilesPath);
            }
        }

        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, int userId, string? subfolder = null)
        {
            var result = new FileUploadResult();

            // Validate file
            var validation = ValidateFile(file);
            if (!validation.IsValid)
            {
                result.ErrorMessage = validation.ErrorMessage;
                return result;
            }

            try
            {
                // Generate unique file name
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var originalFileName = Path.GetFileName(file.FileName);

                // Create user-specific folder structure: ProtectedFiles/{userId}/{subfolder}/
                var userFolder = Path.Combine(_protectedFilesPath, userId.ToString());
                if (!string.IsNullOrEmpty(subfolder))
                {
                    userFolder = Path.Combine(userFolder, subfolder);
                }

                Directory.CreateDirectory(userFolder);

                // Full file path
                var filePath = Path.Combine(userFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return relative path for storage in database
                // Format: {userId}/{subfolder}/{uniqueFileName}
                var relativePath = string.IsNullOrEmpty(subfolder)
                    ? $"{userId}/{uniqueFileName}"
                    : $"{userId}/{subfolder}/{uniqueFileName}";

                result.Success = true;
                result.FilePath = relativePath;
                result.FileName = originalFileName;
                result.FileSize = file.Length;
                result.ContentType = file.ContentType;

                _logger.LogInformation($"File uploaded successfully: {relativePath} by user {userId}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file: {file.FileName}");
                result.ErrorMessage = "An error occurred while uploading the file.";
                return result;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = GetFullFilePath(filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"File deleted: {filePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                return false;
            }
        }

        public Task<FileStream?> GetFileStreamAsync(string filePath)
        {
            try
            {
                var fullPath = GetFullFilePath(filePath);
                if (File.Exists(fullPath))
                {
                    var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return Task.FromResult<FileStream?>(fileStream);
                }
                return Task.FromResult<FileStream?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                return Task.FromResult<FileStream?>(null);
            }
        }

        public FileValidationResult ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "No file was uploaded."
                };
            }

            // Check file size
            if (file.Length > _maxFileSize)
            {
                var maxSizeMB = _maxFileSize / (1024 * 1024);
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File size exceeds the maximum allowed size of {maxSizeMB}MB."
                };
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}"
                };
            }

            // Check MIME type (optional but recommended)
            if (_allowedMimeTypes.TryGetValue(fileExtension, out var allowedMimes))
            {
                if (!allowedMimes.Contains(file.ContentType.ToLowerInvariant()))
                {
                    _logger.LogWarning($"MIME type mismatch for {file.FileName}: Expected {string.Join(" or ", allowedMimes)}, got {file.ContentType}");
                    // Don't fail, just log warning - some browsers may send incorrect MIME types
                }
            }

            return new FileValidationResult { IsValid = true };
        }

        public string[] GetAllowedExtensions()
        {
            return _allowedExtensions;
        }

        public long GetMaxFileSize()
        {
            return _maxFileSize;
        }

        /// <summary>
        /// Get full file path from relative path stored in database
        /// </summary>
        private string GetFullFilePath(string relativePath)
        {
            // Remove any leading slashes or backslashes
            relativePath = relativePath.TrimStart('/', '\\');
            return Path.Combine(_protectedFilesPath, relativePath);
        }

        /// <summary>
        /// Get content type from file extension
        /// </summary>
        public static string GetContentType(string fileExtension)
        {
            return fileExtension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
