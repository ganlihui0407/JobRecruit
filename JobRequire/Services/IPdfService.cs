using JobRequire.Models;

namespace JobRequire.Services
{
    /// <summary>
    /// Service for generating PDF documents
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// Generate PDF from HTML content
        /// </summary>
        /// <param name="htmlContent">HTML content to convert</param>
        /// <param name="title">Document title</param>
        /// <returns>PDF as byte array</returns>
        byte[] GeneratePdfFromHtml(string htmlContent, string title = "Document");

        /// <summary>
        /// Generate resume PDF for a job seeker
        /// </summary>
        /// <param name="jobSeekerProfileId">Job seeker profile ID</param>
        /// <returns>PDF as byte array, or null if not found</returns>
        Task<byte[]?> GenerateResumePdfAsync(int jobSeekerProfileId);

        /// <summary>
        /// Generate Interview Invitation PDF
        /// </summary>
        /// <param name="interview">Interview details</param>
        /// <param name="jobSeekerName">Name of the job seeker</param>
        /// <param name="jobTitle">Position title</param>
        /// <param name="companyName">Company name</param>
        /// <param name="companyAddress">Company address</param>
        /// <returns>PDF as byte array</returns>
        byte[] GenerateInterviewPdf(InterviewDetails interview, string jobSeekerName, string jobTitle, string companyName, string? companyAddress);

        /// <summary>
        /// Generate Offer Letter PDF
        /// </summary>
        /// <param name="offer">Offer details</param>
        /// <param name="jobSeekerName">Name of the job seeker</param>
        /// <param name="companyName">Company name</param>
        /// <param name="companyAddress">Company address</param>
        /// <returns>PDF as byte array</returns>
        byte[] GenerateOfferPdf(OfferDetails offer, string jobSeekerName, string companyName, string? companyAddress);
    }
}
















































