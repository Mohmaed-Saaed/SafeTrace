using SafeTrace.Application.Models.Facebook;

namespace SafeTrace.Application.Services
{
    public class FacebookPostImportService : IFacebookPostImportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FacebookPostImportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int> ImportAsync(
            FacebookPage page,
            IReadOnlyList<FacebookPostDto> posts,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(posts);

            var importedCount = 0;
            var batchPostIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var graphPost in posts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(graphPost.FacebookPostId) ||
                    !batchPostIds.Add(graphPost.FacebookPostId))
                {
                    continue;
                }

                var alreadyImported = await _unitOfWork.Repository<FacebookImportedPost>()
                    .AnyAsync(importedPost =>
                        importedPost.FacebookPageId == page.Id &&
                        importedPost.FacebookPostId == graphPost.FacebookPostId);

                if (alreadyImported)
                    continue;

                var now = DateTime.UtcNow;
                var importedPost = new FacebookImportedPost
                {
                    FacebookPageId = page.Id,
                    FacebookPostId = graphPost.FacebookPostId,
                    PostText = NormalizeOptional(graphPost.PostText),
                    PostUrl = NormalizeOptional(graphPost.PostUrl),
                    PublishedAt = graphPost.PublishedAt,
                    Classification = null,
                    Confidence = null,
                    FName = null,
                    SName = null,
                    TName = null,
                    LName = null,
                    Gender = null,
                    Age = null,
                    Government = null,
                    City = null,
                    Street = null,
                    EventDate = null,
                    CommunicationPhone = null,
                    Description = null,
                    Relation = null,
                    Latitude = null,
                    Longitude = null,
                    LocationAccuracy = null,
                    Status = FacebookImportedPostStatus.PendingAnalysis,
                    CreatedAt = now,
                    Files = graphPost.Media
                        .Where(media => !string.IsNullOrWhiteSpace(media.FileUrl))
                        .DistinctBy(media => media.FileUrl, StringComparer.Ordinal)
                        .Select(media => new FacebookImportedPostFile
                        {
                            FacebookMediaId = NormalizeOptional(media.FacebookMediaId),
                            FileUrl = media.FileUrl.Trim(),
                            CreatedAt = now
                        })
                        .ToList()
                };

                await _unitOfWork.Repository<FacebookImportedPost>()
                    .CreateAsync(importedPost);

                importedCount++;
            }

            if (importedCount > 0)
                await _unitOfWork.SaveAsync();

            return importedCount;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
