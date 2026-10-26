using System.ComponentModel.DataAnnotations;

namespace JobRequire.ViewModels
{
    // =============================================
    // RESUME VIEW MODELS
    // =============================================
    // Purpose: ViewModels for the website resume builder feature.
    // Includes: Dashboard, Profile edit, Education, Experience, Skills.
    // 
    // Design Decision: Each resume section has its own ViewModel for CRUD.
    // ResumeSnapshot is an immutable copy stored with job applications.
    // =============================================

    /// <summary>
    /// ViewModel for the resume builder dashboard.
    /// Shows all resume sections with completion percentage.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Resume/Index
    /// Displays overview of all resume sections.
    /// </remarks>
    public class ResumeDashboardViewModel
    {
        public int JobSeekerProfileId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Summary { get; set; }
        public string? ProfilePhoto { get; set; }
        // ResumeStatus and ResumeLastUpdated properties removed from model
        public int CompletionPercentage { get; set; }

        public List<ResumeEducationViewModel> Education { get; set; } = new();
        public List<ResumeExperienceViewModel> Experience { get; set; } = new();
        public List<ResumeSkillViewModel> Skills { get; set; } = new();

        // Computed properties
        public bool HasEducation => Education.Count > 0;
        public bool HasExperience => Experience.Count > 0;
        public bool HasSkills => Skills.Count > 0;
        public bool IsComplete => HasEducation && HasExperience && HasSkills && !string.IsNullOrEmpty(Summary);
    }

    /// <summary>
    /// ViewModel for editing resume profile information.
    /// Includes personal info and professional summary.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Resume/EditProfile
    /// </remarks>
    public class ResumeProfileViewModel
    {
        public int JobSeekerProfileId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(2000)]
        [Display(Name = "Professional Summary")]
        public string? Summary { get; set; }

        public string? ProfilePhoto { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhotoFile { get; set; }
    }

    /// <summary>
    /// ViewModel for creating/editing education entries in the resume.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /ResumeEducations/Create, /ResumeEducations/Edit/{id}
    /// </remarks>
    public class ResumeEducationViewModel
    {
        public int ResumeEducationId { get; set; }
        public int JobSeekerProfileId { get; set; }

        [Required(ErrorMessage = "School name is required")]
        [StringLength(150)]
        [Display(Name = "School/University")]
        public string SchoolName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Qualification is required")]
        [StringLength(100)]
        [Display(Name = "Qualification/Degree")]
        public string Qualification { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Field of Study")]
        public string? FieldOfStudy { get; set; }

        [Range(1950, 2100)]
        [Display(Name = "Start Year")]
        public int? StartYear { get; set; }

        [Range(1950, 2100)]
        [Display(Name = "End Year")]
        public int? EndYear { get; set; }

        [Display(Name = "Currently Studying")]
        public bool IsCurrentlyStudying { get; set; }

        // Dropdown options
        public List<string> QualificationOptions { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for creating/editing work experience entries in the resume.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /ResumeExperiences/Create, /ResumeExperiences/Edit/{id}
    /// </remarks>
    public class ResumeExperienceViewModel
    {
        public int ResumeExperienceId { get; set; }
        public int JobSeekerProfileId { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job title is required")]
        [StringLength(100)]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Currently Working Here")]
        public bool IsCurrentJob { get; set; }

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// ViewModel for creating/editing skill entries in the resume.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /ResumeSkills/Create, /ResumeSkills/Edit/{id}
    /// </remarks>
    public class ResumeSkillViewModel
    {
        public int ResumeSkillId { get; set; }
        public int JobSeekerProfileId { get; set; }

        [Required(ErrorMessage = "Skill name is required")]
        [StringLength(100)]
        [Display(Name = "Skill Name")]
        public string SkillName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skill level is required")]
        [StringLength(50)]
        [Display(Name = "Proficiency Level")]
        public string SkillLevel { get; set; } = "Intermediate";

        // Dropdown options
        public List<string> SkillLevelOptions { get; set; } = new();
    }

    /// <summary>
    /// Immutable snapshot of resume data stored with job applications.
    /// Preserves the resume state at the time of application submission.
    /// </summary>
    /// <remarks>
    /// Design Decision: Stored as JSON in JobApplication.ResumeSnapshot.
    /// Even if the JobSeeker updates their resume later, the application
    /// retains the original resume that was submitted.
    /// </remarks>
    public class ResumeSnapshotViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Summary { get; set; }
        public string? ProfilePhoto { get; set; }
        /// <summary>
        /// Base64-encoded profile photo for Print/PDF rendering.
        /// Format: "data:image/{type};base64,{data}"
        /// </summary>
        public string? ProfilePhotoBase64 { get; set; }
        public DateTime GeneratedAt { get; set; }

        public List<EducationSnapshot> Education { get; set; } = new();
        public List<ExperienceSnapshot> Experience { get; set; } = new();
        public List<SkillSnapshot> Skills { get; set; } = new();

        /// <summary>
        /// Returns the best available photo source for rendering.
        /// Uses Base64 for print/PDF compatibility, falls back to URL.
        /// </summary>
        public string? GetPhotoSource(bool forPrint = false)
        {
            if (forPrint && !string.IsNullOrEmpty(ProfilePhotoBase64))
                return ProfilePhotoBase64;
            return ProfilePhoto;
        }
    }

    public class EducationSnapshot
    {
        public string SchoolName { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public string? FieldOfStudy { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
    }

    public class ExperienceSnapshot
    {
        public string CompanyName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class SkillSnapshot
    {
        public string SkillName { get; set; } = string.Empty;
        public string SkillLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generic API response for resume operations.
    /// </summary>
    public class ResumeApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class ResumeApiResponse<T> : ResumeApiResponse
    {
        public new T? Data { get; set; }
    }

    /// <summary>
    /// Read-only ViewModel for displaying a JobSeeker's resume to Employers.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Resume/View/{jobSeekerProfileId}
    /// Employers view this when reviewing job applications.
    /// </remarks>
    public class ResumeViewViewModel
    {
        public int JobSeekerProfileId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Summary { get; set; }
        public string? ProfilePhoto { get; set; }

        public List<ResumeEducationViewModel> Education { get; set; } = new();
        public List<ResumeExperienceViewModel> Experience { get; set; } = new();
        public List<ResumeSkillViewModel> Skills { get; set; } = new();

        // Computed properties
        public bool HasEducation => Education.Count > 0;
        public bool HasExperience => Experience.Count > 0;
        public bool HasSkills => Skills.Count > 0;
    }

    /// <summary>
    /// Helper class providing dropdown options for resume forms.
    /// Contains predefined lists for qualifications, skill levels, and common skills.
    /// </summary>
    public static class ResumeOptions
    {
        public static List<string> GetQualifications()
        {
            return new List<string>
            {
                "High School Diploma",
                "Associate Degree",
                "Bachelor's Degree",
                "Master's Degree",
                "Doctorate (PhD)",
                "Professional Certification",
                "Diploma",
                "Certificate",
                "Other"
            };
        }

        public static List<string> GetSkillLevels()
        {
            return new List<string>
            {
                "Beginner",
                "Intermediate",
                "Advanced",
                "Expert"
            };
        }

        public static List<string> GetCommonSkills()
        {
            return new List<string>
            {
                "Microsoft Office",
                "Communication",
                "Problem Solving",
                "Teamwork",
                "Leadership",
                "Project Management",
                "Time Management",
                "Critical Thinking",
                "Data Analysis",
                "Customer Service",
                "Sales",
                "Marketing",
                "Python",
                "JavaScript",
                "Java",
                "C#",
                "SQL",
                "HTML/CSS",
                "React",
                "Node.js",
                "AWS",
                "Azure",
                "Docker",
                "Git"
            };
        }

        public static string GetSkillLevelBadgeClass(string level)
        {
            return level switch
            {
                "Expert" => "bg-success",
                "Advanced" => "bg-primary",
                "Intermediate" => "bg-info",
                "Beginner" => "bg-warning text-dark",
                _ => "bg-secondary"
            };
        }
    }
}
