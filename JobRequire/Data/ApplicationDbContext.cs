using Microsoft.EntityFrameworkCore;
using JobRequire.Models;

namespace JobRequire.Data
{
    /// <summary>
    /// Application database context for the Job Recruitment System.
    /// This is an alias for JobRequireDbContext for conventional naming.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // =============================================
        // DbSet Properties for All Entities
        // =============================================

        // User & Authentication
        public DbSet<User> Users { get; set; }
        public DbSet<FailedLoginAttempt> FailedLoginAttempts { get; set; }
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Profiles
        public DbSet<EmployerProfile> EmployerProfiles { get; set; }
        public DbSet<JobSeekerProfile> JobSeekerProfiles { get; set; }

        // Jobs
        public DbSet<JobVacancy> JobVacancies { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<BookmarkedJob> BookmarkedJobs { get; set; }
        public DbSet<JobInterest> JobInterests { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }

        // Resume Components
        public DbSet<ResumeEducation> ResumeEducations { get; set; }
        public DbSet<ResumeExperience> ResumeExperiences { get; set; }
        public DbSet<ResumeSkill> ResumeSkills { get; set; }
        
        // Application Status History
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }
        
        // Interview & Offer Details
        public DbSet<InterviewDetails> InterviewDetails { get; set; }
        public DbSet<OfferDetails> OfferDetails { get; set; }
        
        // Chat
        public DbSet<ChatThread> ChatThreads { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        
        // Company Reviews
        public DbSet<CompanyReview> CompanyReviews { get; set; }

        // =============================================
        // Model Configuration
        // =============================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================
            // One-to-One Relationships
            // =========================================

            // User <-> EmployerProfile (1:1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.EmployerProfile)
                .WithOne(ep => ep.User)
                .HasForeignKey<EmployerProfile>(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User <-> JobSeekerProfile (1:1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.JobSeekerProfile)
                .WithOne(jsp => jsp.User)
                .HasForeignKey<JobSeekerProfile>(jsp => jsp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================
            // One-to-Many Relationships
            // =========================================

            // EmployerProfile -> JobVacancies (1:N)
            modelBuilder.Entity<EmployerProfile>()
                .HasMany(ep => ep.JobVacancies)
                .WithOne(jv => jv.EmployerProfile)
                .HasForeignKey(jv => jv.EmployerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeekerProfile -> JobApplications (1:N)
            modelBuilder.Entity<JobSeekerProfile>()
                .HasMany(jsp => jsp.JobApplications)
                .WithOne(ja => ja.JobSeekerProfile)
                .HasForeignKey(ja => ja.JobSeekerProfileId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            // JobVacancy -> JobApplications (1:N)
            modelBuilder.Entity<JobVacancy>()
                .HasMany(jv => jv.JobApplications)
                .WithOne(ja => ja.JobVacancy)
                .HasForeignKey(ja => ja.JobVacancyId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeekerProfile -> ResumeEducations (1:N)
            modelBuilder.Entity<JobSeekerProfile>()
                .HasMany(jsp => jsp.ResumeEducations)
                .WithOne(re => re.JobSeekerProfile)
                .HasForeignKey(re => re.JobSeekerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeekerProfile -> ResumeExperiences (1:N)
            modelBuilder.Entity<JobSeekerProfile>()
                .HasMany(jsp => jsp.ResumeExperiences)
                .WithOne(re => re.JobSeekerProfile)
                .HasForeignKey(re => re.JobSeekerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeekerProfile -> ResumeSkills (1:N)
            modelBuilder.Entity<JobSeekerProfile>()
                .HasMany(jsp => jsp.ResumeSkills)
                .WithOne(rs => rs.JobSeekerProfile)
                .HasForeignKey(rs => rs.JobSeekerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeekerProfile -> BookmarkedJobs (1:N)
            modelBuilder.Entity<JobSeekerProfile>()
                .HasMany(jsp => jsp.BookmarkedJobs)
                .WithOne(bj => bj.JobSeekerProfile)
                .HasForeignKey(bj => bj.JobSeekerProfileId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            // JobVacancy -> BookmarkedJobs (1:N)
            modelBuilder.Entity<JobVacancy>()
                .HasMany(jv => jv.BookmarkedJobs)
                .WithOne(bj => bj.JobVacancy)
                .HasForeignKey(bj => bj.JobVacancyId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeekerProfile -> JobInterests (1:N)
            modelBuilder.Entity<JobSeekerProfile>()
                .HasMany(jsp => jsp.JobInterests)
                .WithOne(ji => ji.JobSeekerProfile)
                .HasForeignKey(ji => ji.JobSeekerProfileId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            // JobVacancy -> JobInterests (1:N)
            modelBuilder.Entity<JobVacancy>()
                .HasMany(jv => jv.JobInterests)
                .WithOne(ji => ji.JobVacancy)
                .HasForeignKey(ji => ji.JobVacancyId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> FailedLoginAttempts (1:N)
            modelBuilder.Entity<User>()
                .HasMany(u => u.FailedLoginAttempts)
                .WithOne(fla => fla.User)
                .HasForeignKey(fla => fla.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> EmailVerificationTokens (1:N)
            modelBuilder.Entity<User>()
                .HasMany(u => u.EmailVerificationTokens)
                .WithOne(evt => evt.User)
                .HasForeignKey(evt => evt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> PasswordResetTokens (1:N)
            modelBuilder.Entity<User>()
                .HasMany(u => u.PasswordResetTokens)
                .WithOne(prt => prt.User)
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> ActivityLogs (1:N)
            modelBuilder.Entity<User>()
                .HasMany(u => u.ActivityLogs)
                .WithOne(al => al.User)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Notifications (1:N)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobApplication -> ApplicationDocuments (1:N)
            modelBuilder.Entity<JobApplication>()
                .HasMany(ja => ja.ApplicationDocuments)
                .WithOne(ad => ad.JobApplication)
                .HasForeignKey(ad => ad.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobApplication -> ApplicationStatusHistory (1:N)
            modelBuilder.Entity<JobApplication>()
                .HasMany(ja => ja.StatusHistory)
                .WithOne(sh => sh.JobApplication)
                .HasForeignKey(sh => sh.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> ApplicationStatusHistory (1:N) - Employer who updated the status
            modelBuilder.Entity<User>()
                .HasMany(u => u.StatusHistoryUpdates)
                .WithOne(sh => sh.UpdatedByEmployer)
                .HasForeignKey(sh => sh.UpdatedByEmployerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            // JobApplication -> InterviewDetails (1:N)
            modelBuilder.Entity<JobApplication>()
                .HasMany(ja => ja.InterviewDetails)
                .WithOne(id => id.JobApplication)
                .HasForeignKey(id => id.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobApplication -> OfferDetails (1:N)
            modelBuilder.Entity<JobApplication>()
                .HasMany(ja => ja.OfferDetails)
                .WithOne(od => od.JobApplication)
                .HasForeignKey(od => od.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobApplication -> ChatThread (1:1)
            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.ChatThread)
                .WithOne(ct => ct.JobApplication)
                .HasForeignKey<ChatThread>(ct => ct.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatThread -> ChatMessages (1:N)
            modelBuilder.Entity<ChatThread>()
                .HasMany(ct => ct.Messages)
                .WithOne(cm => cm.ChatThread)
                .HasForeignKey(cm => cm.ChatThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================
            // Unique Constraints / Indexes
            // =========================================

            // Unique username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Unique email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Prevent duplicate bookmarks (same job seeker + same job)
            modelBuilder.Entity<BookmarkedJob>()
                .HasIndex(bj => new { bj.JobSeekerProfileId, bj.JobVacancyId })
                .IsUnique();

            // Prevent duplicate applications (same job seeker + same job)
            modelBuilder.Entity<JobApplication>()
                .HasIndex(ja => new { ja.JobSeekerProfileId, ja.JobVacancyId })
                .IsUnique();

            // Prevent duplicate interests (same job seeker + same job)
            modelBuilder.Entity<JobInterest>()
                .HasIndex(ji => new { ji.JobSeekerProfileId, ji.JobVacancyId })
                .IsUnique();

            // Ensure one chat thread per job application
            modelBuilder.Entity<ChatThread>()
                .HasIndex(ct => ct.JobApplicationId)
                .IsUnique();

            // =========================================
            // Company Review Relationships
            // =========================================

            // EmployerProfile -> CompanyReviews (1:N)
            modelBuilder.Entity<EmployerProfile>()
                .HasMany<CompanyReview>()
                .WithOne(r => r.EmployerProfile)
                .HasForeignKey(r => r.EmployerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeekerProfile -> CompanyReviews (1:N)
            modelBuilder.Entity<JobSeekerProfile>()
                .HasMany<CompanyReview>()
                .WithOne(r => r.JobSeekerProfile)
                .HasForeignKey(r => r.JobSeekerProfileId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            // Prevent duplicate reviews (same job seeker + same company)
            modelBuilder.Entity<CompanyReview>()
                .HasIndex(r => new { r.JobSeekerProfileId, r.EmployerProfileId })
                .IsUnique();
        }
    }
}

