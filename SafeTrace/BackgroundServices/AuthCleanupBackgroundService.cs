using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.API.BackgroundServices
{
    public class AuthCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuthCleanupBackgroundService> _logger;

        public AuthCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AuthCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auth Cleanup Background Service started.");

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var cleanupService = scope.ServiceProvider.GetRequiredService<IAuthCleanupService>();

                    await cleanupService.CleanupExpiredOtpsAsync();
                    await cleanupService.CleanupOldRefreshTokensAsync();

                    _logger.LogInformation("Auth cleanup cycle completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running auth cleanup.");
                }
            }

            _logger.LogInformation("Auth Cleanup Background Service stopped.");
        }
    }
}
