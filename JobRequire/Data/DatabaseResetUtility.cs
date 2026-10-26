using Microsoft.EntityFrameworkCore;

namespace JobRequire.Data
{
    /// <summary>
    /// Utility class to reset database DATA ONLY.
    /// This clears all rows from tables while preserving schema, tables, and relationships.
    /// 
    /// USAGE: Call DatabaseResetUtility.ClearAllDataAsync(dbContext) once, then remove or comment out the call.
    /// </summary>
    public static class DatabaseResetUtility
    {
        /// <summary>
        /// Clears all data from all tables in the correct order to respect foreign key constraints.
        /// Tables are cleared from child tables first, then parent tables.
        /// 
        /// IMPORTANT: This method deletes ALL DATA. Use with caution!
        /// </summary>
        public static async Task ClearAllDataAsync(ApplicationDbContext context)
        {
            // Use raw SQL with TRUNCATE-like behavior via DELETE + RESEED
            // Order matters: Delete from child tables first, then parent tables
            
            // Disable foreign key checks temporarily for clean deletion
            // SQL Server approach: Delete in correct order to respect FK constraints
            
            var tablesToClear = new[]
            {
                // Level 4: Deepest child tables (no dependencies on them)
                "ApplicationDocuments",     // FK: JobApplicationId
                
                // Level 3: Child tables depending on Level 2
                "JobApplications",          // FK: JobSeekerProfileId, JobVacancyId
                "BookmarkedJobs",           // FK: JobSeekerProfileId, JobVacancyId
                "ResumeEducations",         // FK: JobSeekerProfileId
                "ResumeExperiences",        // FK: JobSeekerProfileId
                "ResumeSkills",             // FK: JobSeekerProfileId
                
                // Level 2: Tables depending on User or Profile
                "JobVacancies",             // FK: EmployerProfileId
                "Notifications",            // FK: UserId
                "ActivityLogs",             // FK: UserId
                "FailedLoginAttempts",      // FK: UserId
                "EmailVerificationTokens",  // FK: UserId
                "PasswordResetTokens",      // FK: UserId
                
                // Level 1: Profile tables (depend on Users)
                "EmployerProfiles",         // FK: UserId
                "JobSeekerProfiles",        // FK: UserId
                
                // Level 0: Root table (no FK dependencies)
                "Users"
            };

            foreach (var table in tablesToClear)
            {
                try
                {
                    // Delete all rows from the table
                    await context.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
                    
                    // Reset identity seed to 1 (optional, but nice for clean testing)
                    await context.Database.ExecuteSqlRawAsync($"DBCC CHECKIDENT ('[{table}]', RESEED, 0)");
                    
                    Console.WriteLine($"✓ Cleared table: {table}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error clearing table {table}: {ex.Message}");
                }
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("Database data reset complete!");
            Console.WriteLine("All tables cleared. Schema unchanged.");
            Console.WriteLine("========================================\n");
        }

        /// <summary>
        /// Alternative method using EF Core RemoveRange (slower but type-safe).
        /// Use this if raw SQL approach has issues.
        /// </summary>
        public static async Task ClearAllDataEfCoreAsync(ApplicationDbContext context)
        {
            // Level 4: Deepest children
            context.ApplicationDocuments.RemoveRange(context.ApplicationDocuments);
            await context.SaveChangesAsync();

            // Level 3: Child tables
            context.JobApplications.RemoveRange(context.JobApplications);
            context.BookmarkedJobs.RemoveRange(context.BookmarkedJobs);
            context.ResumeEducations.RemoveRange(context.ResumeEducations);
            context.ResumeExperiences.RemoveRange(context.ResumeExperiences);
            context.ResumeSkills.RemoveRange(context.ResumeSkills);
            await context.SaveChangesAsync();

            // Level 2: User-dependent tables
            context.JobVacancies.RemoveRange(context.JobVacancies);
            context.Notifications.RemoveRange(context.Notifications);
            context.ActivityLogs.RemoveRange(context.ActivityLogs);
            context.FailedLoginAttempts.RemoveRange(context.FailedLoginAttempts);
            context.EmailVerificationTokens.RemoveRange(context.EmailVerificationTokens);
            context.PasswordResetTokens.RemoveRange(context.PasswordResetTokens);
            await context.SaveChangesAsync();

            // Level 1: Profile tables
            context.EmployerProfiles.RemoveRange(context.EmployerProfiles);
            context.JobSeekerProfiles.RemoveRange(context.JobSeekerProfiles);
            await context.SaveChangesAsync();

            // Level 0: Root table
            context.Users.RemoveRange(context.Users);
            await context.SaveChangesAsync();

            Console.WriteLine("\n========================================");
            Console.WriteLine("Database data reset complete (EF Core)!");
            Console.WriteLine("All tables cleared. Schema unchanged.");
            Console.WriteLine("========================================\n");
        }
    }
}











































