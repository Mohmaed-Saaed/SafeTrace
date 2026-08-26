using Microsoft.AspNetCore.Http;
using NetTopologySuite.Geometries;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Request;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.DTOs.Files.Request;

namespace SafeTrace.Application.Services
{
    public sealed class FacebookImportedPostAdminService
        : IFacebookImportedPostAdminService
    {
        private const int PostTextPreviewLength = 180;
        private const int MaxCaseImages = 5;
        private const int UrgentRateLimitMinutes = 2;
        private const int UrgentExpirationHours = 48;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFacebookPostRequirementChecker _requirementChecker;
        private readonly ICaseDuplicateDetectionService _duplicateDetectionService;
        private readonly ICaseHelperService _caseHelper;
        private readonly IExternalImageDownloadService _imageDownloadService;
        private readonly ILogger<FacebookImportedPostAdminService> _logger;

        public FacebookImportedPostAdminService(
            IUnitOfWork unitOfWork,
            IFacebookPostRequirementChecker requirementChecker,
            ICaseDuplicateDetectionService duplicateDetectionService,
            ICaseHelperService caseHelper,
            IExternalImageDownloadService imageDownloadService,
            ILogger<FacebookImportedPostAdminService> logger)
        {
            _unitOfWork = unitOfWork;
            _requirementChecker = requirementChecker;
            _duplicateDetectionService = duplicateDetectionService;
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
            post.ReviewNotes = NormalizeOptional(dto.ReviewNotes);
            post.Latitude = dto.Latitude;
            post.Longitude = dto.Longitude;

            if (coordinatesChanged)
                post.LocationAccuracy = null;

            ApplyReviewStatus(post);
            post.UpdatedAt = DateTime.UtcNow;

            await SaveReviewChangesAsync();
            return MapDetail(post);
        }

        public async Task<FacebookImportedPostDetailDto> RejectAsync(
            long id,
            RejectFacebookImportedPostDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var post = await GetPostAsync(id, tracked: true);
            EnsureReviewIsMutable(post);

            post.Status = FacebookImportedPostStatus.Rejected;
            post.ReviewNotes = NormalizeOptional(dto.ReviewNotes)
                               ?? post.ReviewNotes;
            post.UpdatedAt = DateTime.UtcNow;

            await SaveReviewChangesAsync();
            return MapDetail(post);
        }

        public async Task<PublishFacebookImportedPostResponseDto> PublishAsync(
            long id)
        {
            var post = await GetPostAsync(id, tracked: true);
            ValidatePublishState(post);

            var requirements = _requirementChecker.Evaluate(post);
            if (!requirements.IsComplete)
            {
                ApplyReviewStatus(post);
                post.UpdatedAt = DateTime.UtcNow;
                await SaveReviewChangesAsync();

                throw new BadRequestException(
                    $"The imported post is not ready for publishing. Missing requirements: {string.Join(", ", requirements.MissingRequirements)}.");
            }

            var duplicate = await _duplicateDetectionService.FindDuplicateAsync(post);
            if (duplicate.IsDuplicate && duplicate.ExistingCaseId.HasValue)
            {
                post.Status = FacebookImportedPostStatus.Duplicate;
                post.DuplicateCaseId = duplicate.ExistingCaseId.Value;
                post.UpdatedAt = DateTime.UtcNow;
                await SaveReviewChangesAsync();

                throw new ConflictException(
                    $"A potential duplicate case already exists. CaseId={duplicate.ExistingCaseId}, CaseCode={duplicate.ExistingCaseCode}, CaseType={duplicate.ExistingCaseType}. {duplicate.Reason}");
            }

            var downloadedImages = await DownloadUsableImagesAsync(post.Files);

            try
            {
                var entity = await CreateCaseEntityAsync(post);
                var createdFiles = new List<CaseFile>();

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    createdFiles.AddRange(
                        await _caseHelper.CreateCaseFilesAsync(
                            downloadedImages[0].File,
                            additionalImages: null,
                            video: null,
                            folderName: GetCaseFolder(entity.CaseType),
                            caseId: entity.Id));

                    foreach (var additionalImage in downloadedImages.Skip(1))
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
                    post.DuplicateCaseId = null;
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
                foreach (var image in downloadedImages)
                    image.Dispose();
            }
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

            var requirements = _requirementChecker.Evaluate(post);
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
                    $"The imported post is already marked as a duplicate of CaseId={post.DuplicateCaseId}.");
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

        private async Task<List<DownloadedImageDto>> DownloadUsableImagesAsync(
            IEnumerable<FacebookImportedPostFile> sourceFiles)
        {
            var downloadedImages = new List<DownloadedImageDto>();

            foreach (var sourceFile in sourceFiles
                         .Where(file => !string.IsNullOrWhiteSpace(file.FileUrl))
                         .DistinctBy(file => file.FileUrl, StringComparer.Ordinal))
            {
                if (downloadedImages.Count == MaxCaseImages)
                    break;

                try
                {
                    downloadedImages.Add(
                        await _imageDownloadService.DownloadAsync(sourceFile.FileUrl));
                }
                catch (Exception ex) when (ex is BadRequestException or
                                           HttpRequestException or
                                           TaskCanceledException)
                {
                    _logger.LogWarning(
                        "A Facebook attachment could not be prepared for publishing. ImportedPostFileId={ImportedPostFileId}, ErrorType={ErrorType}",
                        sourceFile.Id,
                        ex.GetType().Name);
                }
            }

            if (downloadedImages.Count == 0)
            {
                throw new BadRequestException(
                    "At least one downloadable supported image is required for publishing.");
            }

            return downloadedImages;
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

            entity.Gender = post.Gender!.Value;
            entity.Government = post.Government!.Trim();
            entity.City = post.City!.Trim();
            entity.Street = post.Street?.Trim() ?? string.Empty;
            entity.FName = NormalizeOptional(post.FName);
            entity.SName = NormalizeOptional(post.SName);
            entity.TName = NormalizeOptional(post.TName);
            entity.LName = NormalizeOptional(post.LName);
            entity.Age = post.Age!.Value;
            entity.UserId = post.FacebookPage.UserId;
            entity.CommunicationPhone = NormalizeOptional(post.CommunicationPhone);
            entity.Status = caseType == CaseType.Urgent
                ? CaseStatus.Active
                : CaseStatus.Pending;
            entity.Relation = post.Relation!.Value;
            entity.CreatedAt = now;
            entity.EventDate = eventDate;
            entity.Description = NormalizeOptional(post.Description);
            entity.CaseType = caseType;
            entity.AgeCategoryId =
                await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(prefix);

            return entity;
        }

        private async Task RollbackAndCleanupAsync(
            IReadOnlyCollection<CaseFile> createdFiles,
            long caseId)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _caseHelper.CleanupPhysicalFiles(
                createdFiles.Select(file => file.ImagePath));

            try
            {
                await _caseHelper.DeleteFacesAsync(
                    createdFiles
                        .Where(file => !string.IsNullOrWhiteSpace(file.FaceId))
                        .Select(file => file.FaceId!),
                    caseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to clean indexed faces after a Facebook publish rollback. CaseId={CaseId}, ErrorType={ErrorType}",
                    caseId,
                    ex.GetType().Name);
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
                    "The imported post was changed by another request. Reload it and try again.");
            }
        }

        private FacebookImportedPostListDto MapListItem(FacebookImportedPost post)
        {
            return new FacebookImportedPostListDto
            {
                Id = post.Id,
                FacebookPageId = post.FacebookPageId,
                FacebookPageName = post.FacebookPage.PageName,
                FacebookPostId = post.FacebookPostId,
                PostUrl = post.PostUrl,
                PostTextPreview = CreatePreview(post.PostText),
                PublishedAt = post.PublishedAt,
                Classification = post.Classification,
                Confidence = post.Confidence,
                FName = post.FName,
                SName = post.SName,
                Age = post.Age,
                Gender = post.Gender,
                Status = post.Status,
                AnalyzedAt = post.AnalyzedAt,
                CreatedAt = post.CreatedAt,
                MissingRequirements =
                    _requirementChecker.Evaluate(post).MissingRequirements
            };
        }

        private FacebookImportedPostDetailDto MapDetail(FacebookImportedPost post)
        {
            return new FacebookImportedPostDetailDto
            {
                Id = post.Id,
                FacebookPageId = post.FacebookPageId,
                FacebookPageName = post.FacebookPage.PageName,
                FacebookPostId = post.FacebookPostId,
                PostText = post.PostText,
                PostUrl = post.PostUrl,
                PublishedAt = post.PublishedAt,
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
                LocationAccuracy = post.LocationAccuracy,
                Files = post.Files
                    .OrderBy(file => file.Id)
                    .Select(file => new FacebookImportedPostFileDto
                    {
                        Id = file.Id,
                        FacebookMediaId = file.FacebookMediaId,
                        FileUrl = file.FileUrl
                    })
                    .ToList(),
                Status = post.Status,
                ReviewNotes = post.ReviewNotes,
                MissingRequirements =
                    _requirementChecker.Evaluate(post).MissingRequirements,
                CaseId = post.CaseId,
                DuplicateCaseId = post.DuplicateCaseId,
                CreatedAt = post.CreatedAt,
                AnalyzedAt = post.AnalyzedAt,
                UpdatedAt = post.UpdatedAt
            };
        }

        private static string? CreatePreview(string? postText)
        {
            if (string.IsNullOrWhiteSpace(postText))
                return null;

            var normalized = string.Join(
                ' ',
                postText.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries));

            return normalized.Length <= PostTextPreviewLength
                ? normalized
                : $"{normalized[..PostTextPreviewLength]}…";
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string GetCaseFolder(CaseType caseType)
        {
            return caseType switch
            {
                CaseType.Urgent => "UrgentCases",
                CaseType.LongTerm => "LongTermCase",
                CaseType.Unknown => "UnknownCase",
                _ => throw new ArgumentOutOfRangeException(nameof(caseType))
            };
        }
    }
}
