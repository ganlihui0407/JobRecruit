using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobRequire.Models
{
    /// <summary>
    /// Represents a company profile for users with the Employer role.
    /// Contains company information displayed on job listings and the company directory.
    /// </summary>
    /// <remarks>
    /// Design Decision: EmployerProfile is separate from User to:
    /// - Keep authentication data separate from profile data
    /// - Allow company-specific fields without cluttering the User entity
    /// - Support future scenarios like multiple admins per company
    /// </remarks>
    public class EmployerProfile
    {
        /// <summary>
        /// Primary key for the EmployerProfile entity.
        /// </summary>
        [Key]
        public int EmployerProfileId { get; set; }

        /// <summary>
        /// Foreign key to the User account that owns this profile.
        /// One-to-one relationship with User.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Company name displayed on job listings and profile.
        /// </summary>
        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Physical address of the company headquarters.
        /// </summary>
        [StringLength(300)]
        public string? CompanyAddress { get; set; }

        /// <summary>
        /// Industry sector (e.g., "Technology", "Finance", "Healthcare").
        /// </summary>
        [StringLength(100)]
        public string? Industry { get; set; }

        /// <summary>
        /// File path or URL to the company logo image.
        /// </summary>
        [StringLength(500)]
        public string? CompanyLogo { get; set; }

        /// <summary>
        /// Detailed description of the company for the public profile.
        /// </summary>
        [StringLength(2000)]
        public string? CompanyDescription { get; set; }

        // =============================================
        // Navigation Properties
        // =============================================

        /// <summary>
        /// The User account associated with this employer profile.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        /// <summary>
        /// Collection of job vacancies posted by this employer.
        /// </summary>
        public ICollection<JobVacancy> JobVacancies { get; set; } = new List<JobVacancy>();
    }
}

