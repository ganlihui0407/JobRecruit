using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Stores offer details when an application status is set to "Offered".
    /// </summary>
    public class OfferDetails
    {
        [Key]
        public int OfferId { get; set; }

        [Required]
        public int JobApplicationId { get; set; }

        [Required]
        [StringLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryOffered { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [StringLength(50)]
        public string EmploymentType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? PdfPath { get; set; }

        // Navigation Property
        [ForeignKey("JobApplicationId")]
        public JobApplication JobApplication { get; set; } = null!;
    }
}


