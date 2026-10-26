using JobRequire.Models;

namespace JobRequire.ViewModels
{
    // =============================================
    // HOME VIEW MODELS
    // =============================================
    // Purpose: ViewModels for the public home page.
    // Includes: Featured jobs by category.
    // =============================================

    /// <summary>
    /// ViewModel for the home page.
    /// Displays featured jobs organized by category.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Home/Index
    /// Shows one featured job per category.
    /// </remarks>
    public class HomeViewModel
    {
        public List<FeaturedCategoryJobViewModel> FeaturedJobs { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for a featured job in a specific category.
    /// Used on the home page to showcase jobs by category.
    /// </summary>
    public class FeaturedCategoryJobViewModel
    {
        public string Category { get; set; } = string.Empty;
        public JobVacancy Job { get; set; } = null!;
        public int InterestedCount { get; set; }
        public bool IsPopular => InterestedCount >= 50;
    }
}








































