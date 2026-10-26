using JobRequire.ViewModels;

namespace JobRequire.Services
{
    public interface IResumeService
    {
        /// <summary>
        /// Generate a JSON snapshot of the resume for job applications
        /// </summary>
        Task<string> GenerateResumeSnapshotJsonAsync(int jobSeekerProfileId);

        /// <summary>
        /// Generate an HTML snapshot of the resume for PDF generation
        /// </summary>
        Task<string> GenerateResumeHtmlAsync(int jobSeekerProfileId);

        /// <summary>
        /// Get the resume snapshot view model
        /// </summary>
        Task<ResumeSnapshotViewModel?> GetResumeSnapshotAsync(int jobSeekerProfileId);

        /// <summary>
        /// Calculate resume completion percentage
        /// </summary>
        Task<int> CalculateCompletionPercentageAsync(int jobSeekerProfileId);

        /// <summary>
        /// Update resume status (Draft/Published)
        /// </summary>
        Task<bool> UpdateResumeStatusAsync(int jobSeekerProfileId, string status);
    }
}

