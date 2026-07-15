using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.API.BackgroundServices;

public class UrgentCaseCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UrgentCaseCleanupBackgroundService> _logger;

    public UrgentCaseCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<UrgentCaseCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Urgent Case Cleanup Background Service started.");

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var cleanupService = scope.ServiceProvider
                    .GetRequiredService<ICaseCleanupService>();

                await cleanupService.CleanupExpiredUrgentCasesAsync();

                _logger.LogInformation("Expired urgent cases cleanup completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning expired urgent cases.");
            }
        }

        _logger.LogInformation("Urgent Case Cleanup Background Service stopped.");
    }
}