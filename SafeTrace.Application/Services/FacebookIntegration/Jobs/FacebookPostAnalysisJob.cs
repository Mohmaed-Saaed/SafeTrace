using Hangfire;
using SafeTrace.Application.DTOs.FacebookPosts.Response;
using SafeTrace.Application.Interfaces.IServices.IFacebookIntegration.IJobs;

namespace SafeTrace.Application.Services.FacebookIntegration.Jobs
{
    public class FacebookPostAnalysisJob : IFacebookPostAnalysisJob
    {
        private const int BatchSize = 20;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiCaseAnalyzerService _aiCaseAnalyzerService;
        private readonly IGeocodingService _geocodingService;
        private readonly IMapper _mapper;
        private readonly ILogger<FacebookPostAnalysisJob> _logger;

        public FacebookPostAnalysisJob(
            IUnitOfWork unitOfWork,
            IAiCaseAnalyzerService aiCaseAnalyzerService,
            IGeocodingService geocodingService,
            IMapper mapper,
            ILogger<FacebookPostAnalysisJob> logger)
        {
            _unitOfWork = unitOfWork;
            _aiCaseAnalyzerService = aiCaseAnalyzerService;
            _geocodingService = geocodingService;
            _mapper = mapper;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ExecuteAsync()
        {
            var posts = await _unitOfWork.Repository<FacebookImportedPost>()
                .Query(tracked: true)
                .Where(post => post.Status == FacebookImportedPostStatus.PendingAnalysis)
                .OrderBy(post => post.Id)
                .Take(BatchSize)
                .ToListAsync();

            _logger.LogInformation(
                "Starting Facebook post analysis. PendingBatchCount={PendingBatchCount}",
                posts.Count);

            foreach (var post in posts)
            {
                if (string.IsNullOrWhiteSpace(post.PostText))
                {
                    post.Status = FacebookImportedPostStatus.Incomplete;
                    post.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation(
                        "Facebook post has no analyzable text. ImportedPostId={ImportedPostId}",
                        post.Id);
                    continue;
                }

                _logger.LogInformation(
                    "Analyzing Facebook post. ImportedPostId={ImportedPostId}",
                    post.Id);

                SocialPostAiResultDto analysis;

                try
                {
                    analysis = await _aiCaseAnalyzerService.AnalyzeAsync(post.PostText);
                }
                catch (Exception ex)
                {
                    post.Status = FacebookImportedPostStatus.AnalysisFailed;
                    post.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveAsync();

                    _logger.LogError(
                        "Facebook post analysis failed. ImportedPostId={ImportedPostId}, ErrorType={ErrorType}",
                        post.Id,
                        ex.GetType().Name);
                    continue;
                }

                _mapper.Map(analysis, post);

                if (post.Classification == SocialPostClassification.Urgent)
                    await TryApplyGeocodingAsync(post);

                var now = DateTime.UtcNow;
                post.AnalyzedAt = now;
                post.UpdatedAt = now;
                post.Status = FacebookImportedPostStatus.NeedsReview;

                await _unitOfWork.SaveAsync();

                _logger.LogInformation(
                    "Facebook post analysis completed. ImportedPostId={ImportedPostId}, Classification={Classification}",
                    post.Id,
                    post.Classification);
            }
        }

        private async Task TryApplyGeocodingAsync(FacebookImportedPost post)
        {
            if (string.IsNullOrWhiteSpace(post.Government) && string.IsNullOrWhiteSpace(post.City))
            {
                return;
            }

            try
            {
                var result = await _geocodingService.GeocodeAsync(
                    post.Government,
                    post.City,
                    post.Street);

                if (result is null)
                {
                    _logger.LogInformation(
                        "No Egypt geocoding result found. ImportedPostId={ImportedPostId}",
                        post.Id);
                    return;
                }

                post.Latitude = result.Latitude;
                post.Longitude = result.Longitude;

                if (string.IsNullOrWhiteSpace(post.Government) &&
                    !string.IsNullOrWhiteSpace(result.Government))
                {
                    post.Government = result.Government;
                }

                _logger.LogInformation(
                    "Egypt geocoding completed. ImportedPostId={ImportedPostId}",
                    post.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Geocoding failed without affecting AI analysis. ImportedPostId={ImportedPostId}, ErrorType={ErrorType}",
                    post.Id,
                    ex.GetType().Name);
            }
        }
    }
}
