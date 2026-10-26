using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobRequire.Services
{
    /// <summary>
    /// Background service that periodically checks for reviews that should be auto-hidden.
    /// Reviews are auto-hidden if:
    /// - Status is UnderReview
    /// - JobseekerResponded is false
    /// - ReportedAt is older than 3 days
    /// </summary>
    public class ReviewAutoHideBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReviewAutoHideBackgroundService> _logger;

        /// <summary>
        /// How often to check for reviews to auto-hide (every 1 hour).
        /// </summary>
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public ReviewAutoHideBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ReviewAutoHideBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReviewAutoHideBackgroundService is starting.");

            // Run immediately on startup
            await ProcessAutoHideAsync();

            // Then run periodically
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    await ProcessAutoHideAsync();
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in ReviewAutoHideBackgroundService");
                    // Continue running even if there's an error
                }
            }

            _logger.LogInformation("ReviewAutoHideBackgroundService is stopping.");
        }

        private async Task ProcessAutoHideAsync()
        {
            _logger.LogDebug("Checking for reviews to auto-hide...");

            using var scope = _serviceProvider.CreateScope();
            var reviewService = scope.ServiceProvider.GetRequiredService<ICompanyReviewService>();

            try
            {
                var hiddenCount = await reviewService.ProcessAutoHideReviewsAsync();
                
                if (hiddenCount > 0)
                {
                    _logger.LogInformation("Auto-hidden {Count} reviews", hiddenCount);
                }
                else
                {
                    _logger.LogDebug("No reviews to auto-hide at this time.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing auto-hide reviews");
            }
        }
    }
}



