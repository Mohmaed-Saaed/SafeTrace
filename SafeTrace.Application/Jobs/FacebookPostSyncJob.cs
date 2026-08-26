using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Exceptions;

namespace SafeTrace.Application.Jobs
{
    public class FacebookPostSyncJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FacebookPostSyncJob> _logger;

        public FacebookPostSyncJob(
            IServiceScopeFactory scopeFactory,
            ILogger<FacebookPostSyncJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Facebook post sync.");

            List<long> pageIds;

            await using (var queryScope = _scopeFactory.CreateAsyncScope())
            {
                var queryUnitOfWork =
                    queryScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                pageIds = await queryUnitOfWork.Repository<FacebookPage>()
                    .Query(tracked: false)
                    .Where(page =>
                        page.IntegrationStatus == FacebookIntegrationStatus.Connected &&
                        page.IsActive)
                    .OrderBy(page => page.Id)
                    .Select(page => page.Id)
                    .ToListAsync(cancellationToken);
            }

            foreach (var pageId in pageIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var pageScope = _scopeFactory.CreateAsyncScope();
                var unitOfWork =
                    pageScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var facebookGraphService =
                    pageScope.ServiceProvider.GetRequiredService<IFacebookGraphService>();
                var importService =
                    pageScope.ServiceProvider.GetRequiredService<IFacebookPostImportService>();
                var page = await unitOfWork.Repository<FacebookPage>().GetByIdAsync(pageId);

                if (page is null ||
                    page.IntegrationStatus != FacebookIntegrationStatus.Connected ||
                    !page.IsActive)
                {
                    continue;
                }

                var syncStartedAt = DateTimeOffset.UtcNow;

                try
                {
                    _logger.LogInformation(
                        "Syncing Facebook page. PageId={PageId}, FacebookPageId={FacebookPageId}",
                        page.Id,
                        page.FacebookPageId);

                    var posts = await facebookGraphService.GetNewPostsAsync(
                        page,
                        page.LastSyncedAt);

                    var importedCount = await importService.ImportAsync(
                        page,
                        posts,
                        cancellationToken);

                    // Checkpoint at the start of a fully successful sync. Posts published
                    // during the request are safely fetched again and deduplicated next run.
                    page.LastSyncedAt = syncStartedAt;
                    page.UpdatedAt = DateTime.UtcNow;
                    await unitOfWork.SaveAsync();

                    _logger.LogInformation(
                        "Facebook page sync completed. PageId={PageId}, FacebookPageId={FacebookPageId}, ImportedCount={ImportedCount}",
                        page.Id,
                        page.FacebookPageId,
                        importedCount);
                }
                catch (FacebookAuthenticationException)
                {
                    try
                    {
                        page.IntegrationStatus = FacebookIntegrationStatus.NeedsReconnect;
                        page.IsActive = false;
                        page.UpdatedAt = DateTime.UtcNow;
                        await unitOfWork.SaveAsync();

                        _logger.LogWarning(
                            "Facebook authorization failed. PageId={PageId}, FacebookPageId={FacebookPageId}. The page now requires reconnection.",
                            page.Id,
                            page.FacebookPageId);
                    }
                    catch (Exception updateException)
                    {
                        _logger.LogError(
                            updateException,
                            "Failed to mark Facebook page for reconnection. PageId={PageId}, FacebookPageId={FacebookPageId}",
                            page.Id,
                            page.FacebookPageId);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Facebook page sync failed. PageId={PageId}, FacebookPageId={FacebookPageId}",
                        page.Id,
                        page.FacebookPageId);
                }
            }

            _logger.LogInformation("Facebook post sync finished.");
        }
    }
}
