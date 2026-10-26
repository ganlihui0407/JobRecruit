using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.Services;
using JobRequire.ViewModels;

namespace JobRequire.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IJobInterestService _interestService;

        public HomeController(
            ILogger<HomeController> logger, 
            ApplicationDbContext context,
            IJobInterestService interestService)
        {
            _logger = logger;
            _context = context;
            _interestService = interestService;
        }

        /// <summary>
        /// Home page - displays landing page with job search and featured jobs
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Get all open jobs with employer profiles and interest counts
            var allJobs = await _context.JobVacancies
                .Include(j => j.EmployerProfile)
                .Include(j => j.JobInterests)
                .Where(j => j.Status == "Open")
                .ToListAsync();

            // Group by category and take the most popular job per category (highest interest count)
            var featuredJobs = allJobs
                .GroupBy(j => j.Category)
                .Select(g => g.OrderByDescending(j => j.JobInterests.Count).ThenByDescending(j => j.PublishDate).First())
                .OrderByDescending(j => j.JobInterests.Count) // Sort categories by most popular job
                .ThenBy(j => j.Category)
                .Select(j => new FeaturedCategoryJobViewModel
                {
                    Category = j.Category,
                    Job = j,
                    InterestedCount = j.JobInterests.Count
                })
                .ToList();

            var viewModel = new HomeViewModel
            {
                FeaturedJobs = featuredJobs
            };

            return View(viewModel);
        }

        /// <summary>
        /// About page - displays information about the job recruitment system
        /// </summary>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Contact page - displays contact information and form
        /// </summary>
        public IActionResult Contact()
        {
            return View();
        }

        /// <summary>
        /// Privacy policy page
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Terms of service page
        /// </summary>
        public IActionResult Terms()
        {
            return View();
        }

        /// <summary>
        /// Error page - displays error information
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }

        /// <summary>
        /// 404 Not Found page
        /// </summary>
        public new IActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}
