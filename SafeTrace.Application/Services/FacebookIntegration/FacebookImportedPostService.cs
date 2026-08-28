using Microsoft.AspNetCore.Http;
using NetTopologySuite.Geometries;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Request;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;
using SafeTrace.Application.DTOs.Files.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Interfaces.IServices.IFacebookIntegration;

namespace SafeTrace.Application.Services.FacebookIntegration
{
    public sealed class FacebookImportedPostService
        : IFacebookImportedPostService
    {
        private const int PostTextPreviewLength = 180;
        private const int MaxCaseImages = 5;
        private const int UrgentRateLimitMinutes = 2;
        private const int UrgentExpirationHours = 48;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly ICaseHelperService _caseHelper;
        private readonly IExternalImageDownloadService _imageDownloadService;
        private readonly ILogger<FacebookImportedPostService> _logger;

        public FacebookImportedPostService(
            IUnitOfWork unitOfWork,
            IFaceRecognitionService faceRecognitionService,
            ICaseHelperService caseHelper,
            IExternalImageDownloadService imageDownloadService,
            ILogger<FacebookImportedPostService> logger)
        {
            _unitOfWork = unitOfWork;
            _faceRecognitionService = faceRecognitionService;
            _caseHelper = caseHelper;
            _imageDownloadService = imageDownloadService;
            _logger = logger;
        }

        public async Task<List<FacebookImportedPostListDto>> GetAllAsync(
            FacebookPostFilterDto filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            var query = _unitOfWork.Repository<FacebookImportedPost>()
                .Query(
                    tracked: false,
                    includes: [post => post.FacebookPage, post => post.Files]);

            if (filter.Status.HasValue)
                query = query.Where(post => post.Status == filter.Status.Value);

            if (filter.Classification.HasValue)
            {
                query = query.Where(post =>
                    post.Classification == filter.Classification.Value);
            }

            if (filter.FacebookPageId.HasValue)
            {
                query = query.Where(post =>
                    post.FacebookPageId == filter.FacebookPageId.Value);
            }

            var posts = await query
                .OrderByDescending(post => post.CreatedAt)
                .ThenByDescending(post => post.Id)
                .ToListAsync();

            return posts.Select(MapListItem).ToList();
        }

        public async Task<FacebookImportedPostDetailDto> GetByIdAsync(long id)
        {
            var post = await GetPostAsync(id, tracked: false);
            return MapDetail(post);
        }

        public async Task<FacebookImportedPostDetailDto> UpdateAsync(
            long id,
            UpdateFacebookImportedPostDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var post = await GetPostAsync(id, tracked: true);
            EnsureReviewIsMutable(post);

            if (dto.Latitude.HasValue != dto.Longitude.HasValue)
            {
                throw new BadRequestException(
                    "Latitude and longitude must be provided together or both cleared.");
            }

            if (dto.EventDate.HasValue &&
                dto.EventDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new BadRequestException(
                    "Event date cannot be in the future.");
            }

            var coordinatesChanged =
                post.Latitude != dto.Latitude ||
                post.Longitude != dto.Longitude;

            post.Classification = dto.Classification;
            post.FName = NormalizeOptional(dto.FName);
            post.SName = NormalizeOptional(dto.SName);
            post.TName = NormalizeOptional(dto.TName);
            post.LName = NormalizeOptional(dto.LName);
            post.Gender = dto.Gender;
            post.Age = dto.Age;
            post.Government = NormalizeOptional(dto.Government);
            post.City = NormalizeOptional(dto.City);
            post.Street = NormalizeOptional(dto.Street);
            post.EventDate = dto.EventDate;
            post.CommunicationPhone = NormalizeOptional(dto.CommunicationPhone);
            post.Description = NormalizeOptional(dto.Description);
            post.Relation = dto.Relation;
            post.Latitude = dto.Latitude;
            post.Longitude = dto.Longitude;

            ApplyReviewStatus(post);
            post.UpdatedAt = DateTime.UtcNow;

            await SaveReviewChangesAsync();
            return MapDetail(post);
        }

        public async Task<FacebookImportedPostDetailDto> RejectAsync(long id)
        {
            var post = await GetPostAsync(id, tracked: true);
            EnsureReviewIsMutable(post);

            post.Status = FacebookImportedPostStatus.Rejected;
            post.UpdatedAt = DateTime.UtcNow;

            await SaveReviewChangesAsync();
            return MapDetail(post);
        }

        public async Task<PublishFacebookImportedPostResponseDto> PublishAsync(
            long id,
            PublishFacebookImportedPostRequestDto? dto = null)
        {
            var post = await GetPostAsync(id, tracked: true);
            ValidatePublishState(post);

            var requirements = ValidateRequirements(post);
            if (!requirements.IsComplete)
            {
                ApplyReviewStatus(post);
                post.UpdatedAt = DateTime.UtcNow;
                await SaveReviewChangesAsync();

                throw new BadRequestException(
                    $"The imported post is not ready for publishing. Missing requirements: {string.Join(", ", requirements.MissingRequirements)}.");
            }

            if (dto is null || dto.PrimaryFileId <= 0)
            {
                throw new BadRequestException("يجب تحديد الصورة الرئيسية للنشر (PrimaryFileId).");
            }

            var primaryFile = post.Files.FirstOrDefault(f => f.Id == dto.PrimaryFileId);
            if (primaryFile is null || primaryFile.FacebookImportedPostId != post.Id)
            {
                throw new BadRequestException("الصورة الرئيسية المحددة غير صالحة أو لا تنتمي إلى هذا المنشور.");
            }

            if (string.IsNullOrWhiteSpace(primaryFile.FileUrl) || !IsValidHttpsUrl(primaryFile.FileUrl))
            {
                throw new BadRequestException("رابط الصورة الرئيسية المحدد غير صالح.");
            }

            using var downloadedPrimaryImage = await _imageDownloadService.DownloadAsync(primaryFile.FileUrl);

            var (existingCase, matchConfidence) = await FindDuplicateCaseAsync(downloadedPrimaryImage.File);
            if (existingCase is not null)
            {
                return new PublishFacebookImportedPostResponseDto
                {
                    FacebookImportedPostId = post.Id,
                    Status = FacebookImportedPostStatus.Duplicate,
                    ExistingCaseId = existingCase.Id,
                    ExistingCaseCode = existingCase.CaseCode,
                    ExistingCaseType = existingCase.CaseType,
                    MatchConfidence = matchConfidence,
                    Message = $"AI face match detected with existing Case {existingCase.CaseCode} ({matchConfidence:F1}% confidence)."
                };
            }

            var additionalFiles = post.Files
                .Where(file => file.Id != primaryFile.Id &&
                               !string.IsNullOrWhiteSpace(file.FileUrl) &&
                               IsValidHttpsUrl(file.FileUrl))
                .DistinctBy(file => file.FileUrl, StringComparer.Ordinal)
                .Take(MaxCaseImages - 1)
                .ToList();

            var additionalDownloadedImages = new List<DownloadedImageDto>();

            try
            {
                foreach (var file in additionalFiles)
                {
                    try
                    {
                        var downloaded = await _imageDownloadService.DownloadAsync(file.FileUrl);
                        additionalDownloadedImages.Add(downloaded);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to download additional Facebook image attachment {FileId}.",
                            file.Id);
                    }
                }

                var entity = await CreateCaseEntityAsync(post);
                var createdFiles = new List<CaseFile>();

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    createdFiles.AddRange(
                        await _caseHelper.CreateCaseFilesAsync(
                            downloadedPrimaryImage.File,
                            additionalImages: null,
                            video: null,
                            folderName: GetCaseFolder(entity.CaseType),
                            caseId: entity.Id));

                    foreach (var additionalImage in additionalDownloadedImages)
                    {
                        createdFiles.AddRange(
                            await _caseHelper.CreateAdditionalCaseFilesAsync(
                                [additionalImage.File],
                                GetCaseFolder(entity.CaseType),
                                entity.Id));
                    }

                    foreach (var file in createdFiles)
                        entity.CaseFiles.Add(file);

                    await _unitOfWork.Repository<Case>().CreateAsync(entity);

                    post.Case = entity;
                    post.Status = FacebookImportedPostStatus.Published;
                    post.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.SaveAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await RollbackAndCleanupAsync(createdFiles, entity.Id);
                    throw new ConflictException(
                        "The imported post was changed or published by another request.");
                }
                catch
                {
                    await RollbackAndCleanupAsync(createdFiles, entity.Id);
                    throw;
                }

                return new PublishFacebookImportedPostResponseDto
                {
                    FacebookImportedPostId = post.Id,
                    CaseId = entity.Id,
                    CaseCode = entity.CaseCode,
                    CaseType = entity.CaseType,
                    Status = post.Status
                };
            }
            finally
            {
                foreach (var image in additionalDownloadedImages)
                    image.Dispose();
            }
        }

        private async Task<(Case? Case, double? Confidence)> FindDuplicateCaseAsync(IFormFile primaryImage)
        {
            List<FaceMatchResult> matches;
            try
            {
                matches = await _faceRecognitionService.SearchByImageAsync(primaryImage);
            }
            catch (BadRequestException ex)
            {
                _logger.LogInformation(
                    "Face recognition search skipped for primary image ({Message}).",
                    ex.Message);
                return (null, null);
            }

            if (matches is null or { Count: 0 })
                return (null, null);

            var distinctFaceIds = matches
                .Where(m => !string.IsNullOrWhiteSpace(m.FaceId) && m.Similarity.HasValue)
                .Select(m => m.FaceId!)
                .Distinct()
                .ToList();

            if (distinctFaceIds.Count == 0)
                return (null, null);

            var matchedCaseFiles = await _unitOfWork.Repository<CaseFile>()
                .Query(tracked: false, includes: [cf => cf.Case])
                .Where(cf => cf.FaceId != null &&
                             distinctFaceIds.Contains(cf.FaceId) &&
                             cf.Case.Status != CaseStatus.Deleted &&
                             cf.Case.Status != CaseStatus.Rejected)
                .ToListAsync();

            if (matchedCaseFiles.Count == 0)
                return (null, null);

            var bestMatch = matches
                .Where(m => matchedCaseFiles.Any(cf => cf.FaceId == m.FaceId))
                .OrderByDescending(m => m.Similarity ?? 0f)
                .FirstOrDefault();

            if (bestMatch is null)
                return (null, null);

            var matchedCase = matchedCaseFiles.First(cf => cf.FaceId == bestMatch.FaceId).Case;
            var confidence = bestMatch.Similarity.HasValue
                ? Math.Round((double)bestMatch.Similarity.Value, 2)
                : (double?)null;

            return (matchedCase, confidence);
        }

        private static FacebookPostRequirementResultDto ValidateRequirements(FacebookImportedPost post)
        {
            ArgumentNullException.ThrowIfNull(post);

            if (!post.Classification.HasValue)
                return new FacebookPostRequirementResultDto(false, ["Classification"]);

            if (post.Classification == SocialPostClassification.Found)
            {
                return new FacebookPostRequirementResultDto(
                    false,
                    ["Found cases require matching with an existing case."]);
            }

            if (post.Classification == SocialPostClassification.NotRelevant)
            {
                return new FacebookPostRequirementResultDto(
                    false,
                    ["Not relevant posts cannot be published as cases."]);
            }

            var missing = new List<string>();

            if (!post.Gender.HasValue)
                missing.Add("Gender");

            if (string.IsNullOrWhiteSpace(post.Government))
                missing.Add("Government");

            if (string.IsNullOrWhiteSpace(post.City))
                missing.Add("City");

            if (!post.Age.HasValue || post.Age is < 1 or > 120)
                missing.Add("Age");

            if (!post.EventDate.HasValue)
                missing.Add("EventDate");

            if (!post.Relation.HasValue || !Enum.IsDefined(post.Relation.Value))
                missing.Add("Relation");

            if (post.FacebookPage is null ||
                string.IsNullOrWhiteSpace(post.FacebookPage.UserId))
            {
                missing.Add("UserId");
            }

            if (!post.Files.Any(file => IsValidHttpsUrl(file.FileUrl)))
                missing.Add("Image");

            if (post.Classification == SocialPostClassification.Urgent)
            {
                if (!post.Latitude.HasValue || post.Latitude is < -90 or > 90)
                    missing.Add("Latitude");

                if (!post.Longitude.HasValue || post.Longitude is < -180 or > 180)
                    missing.Add("Longitude");
            }

            return new FacebookPostRequirementResultDto(missing.Count == 0, missing);
        }

        private static bool IsValidHttpsUrl(string? fileUrl)
        {
            return Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) &&
                   string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<FacebookImportedPost> GetPostAsync(
            long id,
            bool tracked)
        {
            var post = await _unitOfWork.Repository<FacebookImportedPost>()
                .GetOneAsync(
                    item => item.Id == id,
                    tracked,
                    item => item.FacebookPage,
                    item => item.Files);

            return post ?? throw new NotFoundException(
                "Facebook imported post was not found.");
        }

        private void ApplyReviewStatus(FacebookImportedPost post)
        {
            if (post.Classification is SocialPostClassification.Found or
                SocialPostClassification.NotRelevant)
            {
                post.Status = FacebookImportedPostStatus.NeedsReview;
                return;
            }

            var requirements = ValidateRequirements(post);
            post.Status = requirements.IsComplete
                ? FacebookImportedPostStatus.ReadyForPublish
                : FacebookImportedPostStatus.Incomplete;
        }

        private static void EnsureReviewIsMutable(FacebookImportedPost post)
        {
            if (post.Status is FacebookImportedPostStatus.Published or
                FacebookImportedPostStatus.Rejected or
                FacebookImportedPostStatus.Duplicate ||
                post.CaseId.HasValue)
            {
                throw new ConflictException(
                    $"A Facebook imported post in status '{post.Status}' is read-only.");
            }
        }

        private static void ValidatePublishState(FacebookImportedPost post)
        {
            if (post.Status == FacebookImportedPostStatus.Published ||
                post.CaseId.HasValue)
            {
                throw new ConflictException(
                    $"The imported post has already been published as CaseId={post.CaseId}.");
            }

            if (post.Status == FacebookImportedPostStatus.Duplicate)
            {
                throw new ConflictException(
                    "The imported post is marked as duplicate.");
            }

            if (post.Status == FacebookImportedPostStatus.Rejected)
            {
                throw new BadRequestException(
                    "A rejected Facebook imported post cannot be published.");
            }

            if (post.Classification == SocialPostClassification.Found)
            {
                throw new BadRequestException(
                    "Found cases require matching with an existing case and cannot be published by this flow.");
            }

            if (post.Classification == SocialPostClassification.NotRelevant)
            {
                throw new BadRequestException(
                    "Not relevant posts cannot be published as cases.");
            }

            if (post.Status != FacebookImportedPostStatus.ReadyForPublish)
            {
                throw new BadRequestException(
                    $"The imported post must be in '{FacebookImportedPostStatus.ReadyForPublish}' status before publishing. Current status: '{post.Status}'.");
            }
        }

        private async Task<Case> CreateCaseEntityAsync(FacebookImportedPost post)
        {
            var caseType = post.Classification switch
            {
                SocialPostClassification.Urgent => CaseType.Urgent,
                SocialPostClassification.LongTerm => CaseType.LongTerm,
                SocialPostClassification.Unknown => CaseType.Unknown,
                _ => throw new BadRequestException(
                    "This classification cannot be published as a case.")
            };

            var prefix = caseType switch
            {
                CaseType.Urgent => CaseCodePrefix.URG,
                CaseType.LongTerm => CaseCodePrefix.LNG,
                CaseType.Unknown => CaseCodePrefix.UNK,
                _ => throw new ArgumentOutOfRangeException(nameof(caseType))
            };

            var now = DateTime.UtcNow;
            var eventDate = DateTime.SpecifyKind(
                post.EventDate!.Value.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);

            Case entity = caseType switch
            {
                CaseType.Urgent => new UrgentCase
                {
                    Location = new Point(
                        post.Longitude!.Value,
                        post.Latitude!.Value)
                    {
                        SRID = 4326
                    },
                    LimitReachDate = now.AddMinutes(UrgentRateLimitMinutes),
                    EndDate = now.AddHours(UrgentExpirationHours)
                },
                CaseType.LongTerm => new LongTermMissingCase
                {
                    PoliceReportImage = null
                },
                CaseType.Unknown => new UnknownCase(),
                _ => throw new ArgumentOutOfRangeException(nameof(caseType))
            };

            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(prefix);
            entity.CaseType = caseType;
            entity.Status = CaseStatus.Active;
            entity.PreviousStatus = CaseStatus.Pending;
            entity.FName = post.FName!;
            entity.SName = post.SName ?? string.Empty;
            entity.TName = post.TName ?? string.Empty;
            entity.LName = post.LName ?? string.Empty;
            entity.Gender = post.Gender!.Value;
            entity.Age = post.Age!.Value;
            entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
            entity.Government = post.Government!;
            entity.City = post.City!;
            entity.Street = post.Street ?? string.Empty;
            entity.EventDate = eventDate;
            entity.CommunicationPhone = post.CommunicationPhone ?? string.Empty;
            entity.Description = post.Description ?? string.Empty;
            entity.UserId = post.FacebookPage.UserId;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;

            return entity;
        }

        private async Task RollbackAndCleanupAsync(
            IEnumerable<CaseFile> createdFiles,
            long caseId)
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(
                    rollbackException,
                    "Failed to rollback transaction after case creation failure.");
            }

            var caseFiles = createdFiles.ToList();
            if (caseFiles.Count == 0)
                return;

            _caseHelper.CleanupPhysicalFiles(
                caseFiles.Select(file => file.ImagePath));

            var indexedFaceIds = caseFiles
                .Where(file => !string.IsNullOrWhiteSpace(file.FaceId))
                .Select(file => file.FaceId!)
                .ToList();

            if (indexedFaceIds.Count > 0)
            {
                try
                {
                    await _caseHelper.DeleteFacesAsync(indexedFaceIds, caseId);
                }
                catch (Exception faceDeleteException)
                {
                    _logger.LogError(
                        faceDeleteException,
                        "Failed to cleanup indexed faces after publication rollback. CaseId={CaseId}",
                        caseId);
                }
            }
        }

        private async Task SaveReviewChangesAsync()
        {
            try
            {
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "The imported post was modified by another request. Please reload and retry.");
            }
        }

        private static string GetCaseFolder(CaseType caseType) => caseType switch
        {
            CaseType.Urgent => "UrgentCase",
            CaseType.LongTerm => "LongTermCase",
            CaseType.Unknown => "UnknownCase",
            _ => "Case"
        };

        private static FacebookImportedPostListDto MapListItem(FacebookImportedPost post)
        {
            var preview = string.IsNullOrWhiteSpace(post.PostText)
                ? null
                : post.PostText.Length <= PostTextPreviewLength
                    ? post.PostText
                    : $"{post.PostText[..PostTextPreviewLength]}...";

            return new FacebookImportedPostListDto
            {
                Id = post.Id,
                FacebookPageId = post.FacebookPageId,
                FacebookPageName = post.FacebookPage?.PageName ?? string.Empty,
                FacebookPostId = post.FacebookPostId,
                PostUrl = post.PostUrl,
                PublishedAt = post.PublishedAt,
                Status = post.Status,
                Classification = post.Classification,
                Confidence = post.Confidence,
                PostTextPreview = preview,
                FName = post.FName,
                SName = post.SName,
                Age = post.Age,
                Gender = post.Gender,
                CreatedAt = post.CreatedAt,
                AnalyzedAt = post.AnalyzedAt,
                MissingRequirements = ValidateRequirements(post).MissingRequirements
            };
        }

        private static FacebookImportedPostDetailDto MapDetail(FacebookImportedPost post)
        {
            return new FacebookImportedPostDetailDto
            {
                Id = post.Id,
                FacebookPageId = post.FacebookPageId,
                FacebookPageName = post.FacebookPage?.PageName ?? string.Empty,
                FacebookPostId = post.FacebookPostId,
                PostText = post.PostText,
                PostUrl = post.PostUrl,
                PublishedAt = post.PublishedAt,
                Status = post.Status,
                Classification = post.Classification,
                Confidence = post.Confidence,
                FName = post.FName,
                SName = post.SName,
                TName = post.TName,
                LName = post.LName,
                Gender = post.Gender,
                Age = post.Age,
                Government = post.Government,
                City = post.City,
                Street = post.Street,
                EventDate = post.EventDate,
                CommunicationPhone = post.CommunicationPhone,
                Description = post.Description,
                Relation = post.Relation,
                Latitude = post.Latitude,
                Longitude = post.Longitude,
                CaseId = post.CaseId,
                CreatedAt = post.CreatedAt,
                AnalyzedAt = post.AnalyzedAt,
                UpdatedAt = post.UpdatedAt,
                Files = post.Files.Select(file => new FacebookImportedPostFileDto
                {
                    Id = file.Id,
                    FileUrl = file.FileUrl,
                }).ToList()
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
