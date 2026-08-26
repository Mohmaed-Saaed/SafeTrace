using Hangfire;
using SafeTrace.Application.DTOs.AiCaseAnalyzer.Request;
using SafeTrace.Application.DTOs.AiCaseAnalyzer.Response;

namespace SafeTrace.Application.Jobs
{
    public class FacebookPostAnalysisJob
    {
        private const int BatchSize = 20;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiCaseAnalyzerService _aiCaseAnalyzerService;
        private readonly IGeocodingService _geocodingService;
        private readonly ILogger<FacebookPostAnalysisJob> _logger;

        public FacebookPostAnalysisJob(
            IUnitOfWork unitOfWork,
            IAiCaseAnalyzerService aiCaseAnalyzerService,
            IGeocodingService geocodingService,
            ILogger<FacebookPostAnalysisJob> logger)
        {
            _unitOfWork = unitOfWork;
            _aiCaseAnalyzerService = aiCaseAnalyzerService;
            _geocodingService = geocodingService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var posts = await _unitOfWork.Repository<FacebookImportedPost>()
                .Query(tracked: true)
                .Where(post => post.Status == FacebookImportedPostStatus.PendingAnalysis)
                .OrderBy(post => post.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Starting Facebook post analysis. PendingBatchCount={PendingBatchCount}",
                posts.Count);

            foreach (var post in posts)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
                    analysis = await _aiCaseAnalyzerService.AnalyzeAsync(
                        new SocialPostAiInputDto
                        {
                            Text = post.PostText
                        });
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
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

                ApplyAnalysis(post, analysis);

                if (post.Classification == SocialPostClassification.Urgent)
                    await TryApplyGeocodingAsync(post, cancellationToken);

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

        private async Task TryApplyGeocodingAsync(
            FacebookImportedPost post,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(post.Government) &&
                string.IsNullOrWhiteSpace(post.City))
            {
                return;
            }

            try
            {
                var result = await _geocodingService.GeocodeAsync(
                    post.Government,
                    post.City,
                    post.Street,
                    cancellationToken);

                if (result is null)
                {
                    _logger.LogInformation(
                        "No Egypt geocoding result found. ImportedPostId={ImportedPostId}",
                        post.Id);
                    return;
                }

                post.Latitude = result.Latitude;
                post.Longitude = result.Longitude;
                post.LocationAccuracy = result.LocationAccuracy;

                if (string.IsNullOrWhiteSpace(post.Government) &&
                    !string.IsNullOrWhiteSpace(result.Government))
                {
                    post.Government = result.Government;
                }

                _logger.LogInformation(
                    "Egypt geocoding completed. ImportedPostId={ImportedPostId}, LocationAccuracy={LocationAccuracy}",
                    post.Id,
                    result.LocationAccuracy);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Geocoding failed without affecting AI analysis. ImportedPostId={ImportedPostId}, ErrorType={ErrorType}",
                    post.Id,
                    ex.GetType().Name);
            }
        }

        private static void ApplyAnalysis(
            FacebookImportedPost post,
            SocialPostAiResultDto analysis)
        {
            post.Classification = analysis.Classification;
            post.Confidence = analysis.Confidence;
            post.FName = analysis.Person.FirstName;
            post.SName = analysis.Person.SecondName;
            post.TName = analysis.Person.ThirdName;
            post.LName = analysis.Person.LastName;
            post.Gender = analysis.Person.Gender;
            post.Age = analysis.Person.Age;
            post.Government = analysis.MissingInfo.Government;
            post.City = analysis.MissingInfo.City;
            post.Street = analysis.MissingInfo.Street;
            post.EventDate = analysis.MissingInfo.EventDate;
            post.CommunicationPhone = analysis.Contact.Phone;
            post.Description = analysis.Description;
            post.Relation = analysis.Relation;
        }
    }
}
