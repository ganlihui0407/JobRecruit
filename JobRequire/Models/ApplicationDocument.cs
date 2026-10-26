using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents an additional document attached to a job application.
    /// Examples: cover letters, certificates, portfolios.
    /// </summary>
    public class ApplicationDocument
    {
        /// <summary>
        /// Primary key for the ApplicationDocument entity.
        /// </summary>
        [Key]
        public int ApplicationDocumentId { get; set; }

        /// <summary>
        /// Foreign key to the JobApplication this document belongs to.
        /// </summary>
        [Required]
        public int JobApplicationId { get; set; }

        /// <summary>
        /// Server file path where the document is stored.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Original filename as uploaded by the user.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the file (e.g., "application/pdf", "image/jpeg").
        /// </summary>
        [StringLength(100)]
        public string? ContentType { get; set; }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Timestamp when the document was uploaded (UTC).
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The job application this document is attached to.
        /// </summary>
        [ForeignKey("JobApplicationId")]
        public JobApplication JobApplication { get; set; } = null!;
    }
}

