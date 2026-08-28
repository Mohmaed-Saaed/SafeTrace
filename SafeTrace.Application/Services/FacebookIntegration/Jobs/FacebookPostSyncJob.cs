using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.DTOs.FacebookPosts.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.IFacebookIntegration.IJobs;

namespace SafeTrace.Application.Services.FacebookIntegration.Jobs
{
    public class FacebookPostSyncJob : IFacebookPostSyncJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<FacebookPostSyncJob> _logger;

        public FacebookPostSyncJob(
            IServiceScopeFactory scopeFactory,
            IMapper mapper,
            ILogger<FacebookPostSyncJob> logger)
        {
            _scopeFactory = scopeFactory;
            _mapper = mapper;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting Facebook post sync.");

            List<long> pageIds;

            await using (var queryScope = _scopeFactory.CreateAsyncScope())
            {
                var queryUnitOfWork = queryScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                pageIds = await queryUnitOfWork.Repository<FacebookPage>()
                    .Query(tracked: false)
                    .Where(page => page.IntegrationStatus == FacebookIntegrationStatus.Connected && page.IsActive)
                    .OrderBy(page => page.Id)
                    .Select(page => page.Id)
                    .ToListAsync();
            }

            foreach (var pageId in pageIds)
            {
                await using var pageScope = _scopeFactory.CreateAsyncScope();
                var unitOfWork = pageScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var facebookGraphService = pageScope.ServiceProvider.GetRequiredService<IFacebookGraphService>();

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
                        page.FacebookPageId,
                        page.PageAccessToken!,
                        page.LastSyncedAt);


                    var importedCount = await ImportPostsAsync(
                        page,
                        posts,
                        unitOfWork);

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

        private async Task<int> ImportPostsAsync(
            FacebookPage page,
            IReadOnlyList<FacebookPostDto> posts,
            IUnitOfWork unitOfWork)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(posts);

            var importedCount = 0;
            var batchPostIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var graphPost in posts)
            {
                if (string.IsNullOrWhiteSpace(graphPost.FacebookPostId) ||
                    !batchPostIds.Add(graphPost.FacebookPostId))
                {
                    continue;
                }

                var alreadyImported = await unitOfWork.Repository<FacebookImportedPost>()
                    .AnyAsync(importedPost =>
                        importedPost.FacebookPageId == page.Id &&
                        importedPost.FacebookPostId == graphPost.FacebookPostId);

                if (alreadyImported)
                    continue;

                var now = DateTime.UtcNow;
                var importedPost = _mapper.Map<FacebookImportedPost>(graphPost);
                importedPost.FacebookPageId = page.Id;
                importedPost.CreatedAt = now;

                await unitOfWork.Repository<FacebookImportedPost>().CreateAsync(importedPost);

                importedCount++;
            }

            if (importedCount > 0)
                await unitOfWork.SaveAsync();

            return importedCount;
        }
    }
}
