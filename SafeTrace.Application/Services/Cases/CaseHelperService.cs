using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Constants;
using Hangfire;

namespace SafeTrace.Application.Services.Cases
{
    public class CaseHelperService : ICaseHelperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly INotificationServices _notificationServices;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<CaseHelperService> _logger;

        private const float MinimumSimilarity = 80f;

        public CaseHelperService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IFaceRecognitionService faceRecognitionService,
            INotificationServices notificationServices,
            IEmailService emailService,
            IMapper mapper,
            ILogger<CaseHelperService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _faceRecognitionService = faceRecognitionService;
            _notificationServices = notificationServices;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TEntity> GetValidCaseAsync<TEntity>(
            long id,
            bool allowDeleted = false,
            bool tracked = true,
            params Expression<Func<TEntity, object>>[] includes)
            where TEntity : Case
        {
            var entity = await _unitOfWork.Repository<TEntity>().GetOneAsync(
                x => x.Id == id && (allowDeleted || x.Status != CaseStatus.Deleted),
                tracked: tracked,
                includes: includes);

            if (entity == null)
            {
                throw new NotFoundException("الحالة غير موجودة.");
            }

            return entity;
        }

        public void ValidateCaseIsEditable(Case entity)
        {
            if (entity.Status == CaseStatus.Found || entity.Status == CaseStatus.Expired || entity.Status == CaseStatus.Deleted)
            {
                throw new BadRequestException($"لا يمكن تنفيذ هذا الإجراء على حالة بحالة '{entity.Status}'.");
            }
        }

        public async Task ValidateVerifiedUserAsync(string userId)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>()
                .GetOneAsync(u => u.Id == userId, tracked: false);

            if (user is null)
                throw new NotFoundException("المستخدم غير موجود.");

            if (user.VerificationStatus != VerificationStatus.Verified)
                throw new UnauthorizedException("يجب توثيق حسابك قبل تنفيذ هذا الإجراء.");
        }

        public async Task<int> ResolveAgeCategoryIdAsync(int age)
        {
            var category = await _unitOfWork.Repository<AgeCategory>()
                .GetOneAsync(c => age >= c.MinAge && age <= c.MaxAge, tracked: false);

            if (category is null)
                throw new NotFoundException($"لا يوجد فئة عمرية محددة للعمر {age}.");

            return category.Id;
        }

        public async Task<string> GenerateCaseCodeAsync(CaseCodePrefix prefix)
        {
            if (!SequenceNames.TryGetValue(prefix, out var sequenceName))
                throw new ArgumentOutOfRangeException(nameof(prefix), "البادئة غير صالحة.");

            var number = await _unitOfWork.GetNextSequenceValueAsync(sequenceName);

            return $"{prefix}-{number}";
        }

        private static readonly Dictionary<CaseCodePrefix, string> SequenceNames = new()
        {
            { CaseCodePrefix.LNG, "LongTermCaseSequence" },
            { CaseCodePrefix.URG, "UrgentCaseSequence" },
            { CaseCodePrefix.UNK, "UnknownCaseSequence" }
        };

        public async Task<List<CaseFile>> CreateCaseFilesAsync(
            IFormFile primaryImage,
            IEnumerable<IFormFile>? additionalImages,
            IFormFile? video,
            string folderName,
            long caseId = 0,
            bool requireFaceIndexing = false)
        {
            var files = new List<CaseFile>();

            try
            {
                files.Add(await CreateCaseFileAsync(
                    primaryImage,
                    folderName,
                    caseId,
                    true,
                    requireFaceIndexing));

                if (additionalImages != null)
                {
                    foreach (var image in additionalImages)
                    {
                        files.Add(await CreateCaseFileAsync(
                            image,
                            folderName,
                            caseId,
                            false,
                            requireFaceIndexing));
                    }
                }

                if (video != null)
                {
                    files.Add(await CreateCaseFileAsync(
                        video,
                        folderName,
                        caseId,
                        false,
                        false));
                }

                return files;
            }
            catch when (requireFaceIndexing)
            {
                CleanupPhysicalFiles(files.Select(x => x.ImagePath));
                await DeleteFacesAsync(
                    files.Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                        .Select(x => x.FaceId!),
                    caseId);
                throw;
            }
        }

        public async Task<List<CaseFile>> CreateAdditionalCaseFilesAsync(
            IEnumerable<IFormFile> images,
            string folderName,
            long caseId = 0,
            bool requireFaceIndexing = false)
        {
            var files = new List<CaseFile>();

            try
            {
                foreach (var image in images)
                {
                    var file = await CreateCaseFileAsync(
                        image,
                        folderName,
                        caseId,
                        false,
                        requireFaceIndexing);

                    file.IsPrimary = false;

                    files.Add(file);
                }

                return files;
            }
            catch when (requireFaceIndexing)
            {
                CleanupPhysicalFiles(files.Select(x => x.ImagePath));
                await DeleteFacesAsync(
                    files.Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                        .Select(x => x.FaceId!),
                    caseId);
                throw;
            }
        }

        private async Task<CaseFile> CreateCaseFileAsync(
            IFormFile file,
            string folderName,
            long caseId,
            bool isPrimary,
            bool requireFaceIndexing = false)
        {
            var path = await _fileStorageService.SaveFileAsync(file, folderName);

            string? faceId = null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var fileType = VideoExtensions.Contains(extension) ? FileType.Video : FileType.Image;

            if (fileType == FileType.Image)
            {
                try
                {
                    faceId = await _faceRecognitionService.IndexFaceAsync(file);
                }
                catch (Exception ex)
                {
                    if (requireFaceIndexing)
                    {
                        CleanupPhysicalFiles([path]);
                        _logger.LogError(
                            ex,
                            "Failed to index a required face for update file {FileName}.",
                            file.FileName);
                        throw;
                    }

                    _logger.LogWarning(ex, "Failed to index face for file {FileName}.", file.FileName);
                }
            }

            if (requireFaceIndexing && string.IsNullOrWhiteSpace(faceId))
            {
                CleanupPhysicalFiles([path]);
                throw new BadRequestException("Cannot index uploaded image face.");
            }

            return new CaseFile
            {
                CaseId = caseId,
                ImagePath = path,
                FaceId = faceId,
                IsPrimary = isPrimary,
                Type= fileType,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void ValidateUpdateMediaState(
            IEnumerable<CaseFile> existingFiles,
            IFormFile? newPrimaryImage,
            IEnumerable<IFormFile>? newAdditionalImages,
            IEnumerable<long>? deletedPhotoIds)
        {
            var allFiles = existingFiles.ToList();
            var currentImages = allFiles
                .Where(x => x.Type == FileType.Image)
                .ToList();
            var additionalUploads = newAdditionalImages?.ToList() ?? [];
            var deletionIds = deletedPhotoIds?.ToList() ?? [];

            if (deletionIds.Count != deletionIds.Distinct().Count())
            {
                throw new BadRequestException("Deleted photo IDs must be distinct.");
            }

            foreach (var deletedId in deletionIds)
            {
                var file = allFiles.FirstOrDefault(x => x.Id == deletedId);

                if (file is null)
                {
                    throw new BadRequestException(
                        "Deleted photo ID does not belong to this case.");
                }

                if (file.Type != FileType.Image)
                {
                    throw new BadRequestException(
                        "DeletedPhotoIds can only reference image files.");
                }
            }

            var deletedIdSet = deletionIds.ToHashSet();
            int finalPrimaryCount;
            int finalAdditionalCount;

            if (newPrimaryImage is not null)
            {
                // A dedicated primary upload replaces every existing primary. Existing
                // additional images stay additional; none is promoted as a fallback.
                finalPrimaryCount = 1;
                finalAdditionalCount = currentImages.Count(x =>
                    !x.IsPrimary && !deletedIdSet.Contains(x.Id)) +
                    additionalUploads.Count;
            }
            else
            {
                var retainedImages = currentImages
                    .Where(x => !deletedIdSet.Contains(x.Id))
                    .ToList();

                finalPrimaryCount = retainedImages.Count(x => x.IsPrimary);
                finalAdditionalCount = retainedImages.Count(x => !x.IsPrimary) +
                    additionalUploads.Count;
            }

            if (finalPrimaryCount == 0)
            {
                throw new BadRequestException("Primary image is required.");
            }

            if (finalPrimaryCount > 1)
            {
                throw new BadRequestException("Only one primary image is allowed.");
            }

            if (finalAdditionalCount > 4)
            {
                throw new BadRequestException(
                    "A case can have a maximum of four additional images.");
            }

            var hadReliableAnchor = currentImages.Any(
                x => !string.IsNullOrWhiteSpace(x.FaceId));
            var retainsReliableAnchor = currentImages.Any(x =>
                !deletedIdSet.Contains(x.Id) &&
                !(newPrimaryImage is not null && x.IsPrimary) &&
                !string.IsNullOrWhiteSpace(x.FaceId));
            var willIndexNewAnchor = newPrimaryImage is not null ||
                additionalUploads.Count > 0;

            if (hadReliableAnchor && !retainsReliableAnchor && !willIndexNewAnchor)
            {
                throw new BadRequestException(
                    "Cannot remove the case's last verifiable identity image.");
            }
        }

        public void ValidateFinalUpdateIdentityAnchor(
            IEnumerable<CaseFile> existingFiles,
            IEnumerable<CaseFile> stagedNewFiles,
            IEnumerable<long>? deletedPhotoIds,
            bool replacesPrimary)
        {
            var existingImages = existingFiles
                .Where(x => x.Type == FileType.Image)
                .ToList();

            if (!existingImages.Any(x => !string.IsNullOrWhiteSpace(x.FaceId)))
                return;

            var deletedIdSet = deletedPhotoIds?.ToHashSet() ?? [];
            var retainsAnchor = existingImages.Any(x =>
                !deletedIdSet.Contains(x.Id) &&
                !(replacesPrimary && x.IsPrimary) &&
                !string.IsNullOrWhiteSpace(x.FaceId));
            var hasIndexedNewAnchor = stagedNewFiles.Any(x =>
                x.Type == FileType.Image &&
                !string.IsNullOrWhiteSpace(x.FaceId));

            if (!retainsAnchor && !hasIndexedNewAnchor)
            {
                throw new BadRequestException(
                    "Cannot remove the case's last verifiable identity image.");
            }
        }

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".mov",
            ".webm"
        };

        public void CleanupPhysicalFiles(IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    _fileStorageService.DeleteFile(path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete physical file {FilePath}", path);
                }
            }
        }

        public async Task DeleteFacesAsync(IEnumerable<string>? faceIds, long caseId)
        {
            var ids = faceIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? [];

            if (ids.Count == 0)
                return;

            try
            {
                await _faceRecognitionService.DeleteFacesAsync(ids);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete faces for case {CaseId}", caseId);

                // Cleanup happens after commit (or as compensation for failed staging),
                // so a transient AI outage must not leave stale face IDs forever.
                // Hangfire's automatic retries handle the external cleanup separately.
                try
                {
                    BackgroundJob.Enqueue<IFaceRecognitionService>(
                        service => service.DeleteFacesAsync(ids));
                }
                catch (Exception enqueueException)
                {
                    _logger.LogError(
                        enqueueException,
                        "Failed to enqueue face cleanup retry for case {CaseId}",
                        caseId);
                }
            }
        }

        public async Task SendCaseApprovedNotificationAsync(Case entity)
        {
            var user = await GetCaseOwnerAsync(entity);
            if (user is null)
                return;

            var detailsPath = EmailTemplates.GetCaseDetailsRoute(entity.CaseType);
            var directLink = $"{detailsPath}{entity.Id}?action=approved&caseId={entity.Id}&caseCode={Uri.EscapeDataString(entity.CaseCode ?? string.Empty)}";

            await SendNotificationSafelyAsync(
                user.Id,
                $"✅ تمت الموافقة على حالتك.\n\nكود الحالة:\n{entity.CaseCode}\n\nيمكنك الآن البحث عن الحالة باستخدام كود الحالة أو متابعة تفاصيلها.",
                directLink,
                entity.Id,
                "approved");

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await SendEmailSafelyAsync(
                    user.Email,
                    "تمت الموافقة على حالتك",
                    EmailTemplates.BuildCaseApprovedEmailTemplate(
                        $"{user.FName} {user.LName}".Trim(),
                        entity.CaseCode,
                        GetCaseTypeName(entity.CaseType),
                        EmailTemplates.GetCaseDetailsUrl(entity.CaseType, entity.Id)),
                    entity.Id,
                    "approved");
            }
        }

        public async Task SendCaseRejectedNotificationAsync(Case entity, string rejectionReason)
        {
            var user = await GetCaseOwnerAsync(entity);
            if (user is null)
                return;

            var detailsPath = EmailTemplates.GetCaseDetailsRoute(entity.CaseType);
            var directLink = $"{detailsPath}{entity.Id}?action=rejected&caseId={entity.Id}&caseCode={Uri.EscapeDataString(entity.CaseCode ?? string.Empty)}&rejectionReason={Uri.EscapeDataString(rejectionReason ?? string.Empty)}";

            await SendNotificationSafelyAsync(
                user.Id,
                $"❌ تم رفض الحالة.\n\nسبب الرفض:\n\n{rejectionReason}",
                directLink,
                entity.Id,
                "rejected");

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await SendEmailSafelyAsync(
                    user.Email,
                    "تم رفض الحالة",
                    EmailTemplates.BuildCaseRejectedEmailTemplate(
                        $"{user.FName} {user.LName}".Trim(),
                        entity.CaseCode,
                        rejectionReason,
                        EmailTemplates.GetCaseDetailsUrl(entity.CaseType, entity.Id)),
                    entity.Id,
                    "rejected");
            }
        }

        private async Task<ApplicationUser?> GetCaseOwnerAsync(Case entity)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>()
                .GetOneAsync(user => user.Id == entity.UserId, tracked: false);

            if (user is null)
                _logger.LogWarning("Could not send case notification because owner {UserId} was not found for case {CaseId}.", entity.UserId, entity.Id);

            return user;
        }

        private async Task SendNotificationSafelyAsync(string userId, string content, string detailsPath, long caseId, string action)
        {
            try
            {
                await _notificationServices.SendNotificationAsync(new SendNotificationDTO
                {
                    UserId = userId,
                    Content = content,
                    Type = NotificationType.Message,
                    NotificationDirectLink = detailsPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Action} in-app notification for case {CaseId}.", action, caseId);
            }
        }

        private Task SendEmailSafelyAsync(string email, string subject, string body, long caseId, string action)
        {
            try
            {
                BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(email, subject, body));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Action} email for case {CaseId}.", action, caseId);
            }
            return Task.CompletedTask;
        }

        private static string GetCaseTypeName(CaseType caseType) => caseType switch
        {
            CaseType.LongTerm => "حالة فقد طويلة المدة",
            CaseType.Urgent => "حالة عاجلة",
            CaseType.Unknown => "حالة مجهول الهوية",
            _ => caseType.ToString()
        };

        private async Task<List<MatchedCaseDto>> FindMatchedCasesAsync(IFormFile primaryImage)
        {
            var faceMatches = await SearchFacesAsync(primaryImage);
            if (faceMatches.Count == 0)
                return [];

            var candidateCases = await LoadCandidateCasesAsync(faceMatches);
            if (candidateCases.Count == 0)
                return [];

            return FilterMatchedCases(candidateCases, faceMatches);
        }

        public async Task<DuplicateCheckResult> CheckDuplicateCaseAsync(
            CaseType currentCaseType,
            IFormFile primaryImage,
            string currentUserId)
        {
            var matchedCases = await FindMatchedCasesAsync(primaryImage);

            if (matchedCases.Count == 0)
            {
                return new DuplicateCheckResult { IsBlocked = false, DuplicateDecision = DuplicateDecision.None, MatchedCases = [] };
            }

            var sameUserMatch = matchedCases.FirstOrDefault(c => c.UserId == currentUserId);
            if (sameUserMatch != null)
            {
                return new DuplicateCheckResult
                {
                    IsBlocked = true,
                    DuplicateDecision = DuplicateDecision.SameUserDuplicate,
                    ExistingCaseId = sameUserMatch.Id,
                    ExistingCaseType = sameUserMatch.CaseType,
                    ExistingStatus = sameUserMatch.Status,
                    MatchedCases = []
                };
            }

            var pendingOwner = matchedCases.FirstOrDefault(c => c.Status == CaseStatus.Pending && (c.CaseType == CaseType.Urgent || c.CaseType == CaseType.LongTerm));
            if (pendingOwner != null)
            {
                return new DuplicateCheckResult
                {
                    IsBlocked = true,
                    DuplicateDecision = DuplicateDecision.PendingOwnerCase,
                    ExistingCaseId = pendingOwner.Id,
                    ExistingCaseType = pendingOwner.CaseType,
                    ExistingStatus = pendingOwner.Status,
                    MatchedCases = []
                };
            }

            var pendingUnknown = matchedCases.FirstOrDefault(c => c.Status == CaseStatus.Pending && c.CaseType == CaseType.Unknown);
            if (pendingUnknown != null)
            {
                return new DuplicateCheckResult
                {
                    IsBlocked = false,
                    DuplicateDecision = DuplicateDecision.PendingUnknownCase,
                    ExistingCaseId = pendingUnknown.Id,
                    ExistingCaseType = pendingUnknown.CaseType,
                    ExistingStatus = pendingUnknown.Status,
                    MatchedCases = []
                };
            }

            var activeOwner = matchedCases.FirstOrDefault(c => c.Status == CaseStatus.Active && (c.CaseType == CaseType.Urgent || c.CaseType == CaseType.LongTerm));
            if (activeOwner != null)
            {
                return new DuplicateCheckResult
                {
                    IsBlocked = true,
                    DuplicateDecision = DuplicateDecision.ActiveOwnerCase,
                    ExistingCaseId = activeOwner.Id,
                    ExistingCaseType = activeOwner.CaseType,
                    ExistingStatus = activeOwner.Status,
                    MatchedCases = matchedCases
                };
            }

            var activeUnknown = matchedCases.FirstOrDefault(c => c.Status == CaseStatus.Active && c.CaseType == CaseType.Unknown);
            if (activeUnknown != null)
            {
                return new DuplicateCheckResult
                {
                    IsBlocked = false,
                    DuplicateDecision = DuplicateDecision.ActiveUnknownCase,
                    ExistingCaseId = activeUnknown.Id,
                    ExistingCaseType = activeUnknown.CaseType,
                    ExistingStatus = activeUnknown.Status,
                    MatchedCases = matchedCases
                };
            }

            return new DuplicateCheckResult { IsBlocked = false, DuplicateDecision = DuplicateDecision.None, MatchedCases = [] };
        }

        public async Task ValidateUploadedImagesIdentityAsync(
            IFormFile? primaryImage,
            IEnumerable<IFormFile>? additionalImages,
            IEnumerable<string>? existingFaceIds = null)
        {
            var images = new List<IFormFile>();

            if (primaryImage != null)
                images.Add(primaryImage);

            if (additionalImages != null)
                images.AddRange(additionalImages);

            if (images.Count == 0)
                return;

            // A null anchor collection is the existing create-flow contract. Update
            // callers explicitly pass the case's anchors (including an empty list),
            // which lets update fail closed without introducing duplicate detection.
            if (existingFaceIds is null)
                return;

            var reliableFaceIds = existingFaceIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.Ordinal);

            if (reliableFaceIds.Count == 0)
            {
                throw new BadRequestException(
                    "Cannot verify uploaded image identity.");
            }

            foreach (var image in images)
            {
                var matches = await _faceRecognitionService.SearchByImageAsync(image);

                var isSamePerson = matches.Any(m =>
                    reliableFaceIds.Contains(m.FaceId) &&
                    PassesVerification(m.Similarity ?? 0));


                if (!isSamePerson)
                {
                    throw new BadRequestException(
                        "الصورة الجديدة لا تبدو لنفس الشخص الموجود في هذا البلاغ.");
                }
            }
        }

        private async Task<List<FaceMatchResult>> SearchFacesAsync(IFormFile primaryImage)
        {
            var faceMatches = await _faceRecognitionService.SearchByImageAsync(primaryImage);

            if (faceMatches == null || faceMatches.Count == 0)
                return [];

            return faceMatches
                .Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                .GroupBy(x => x.FaceId)
                .Select(g => new FaceMatchResult { FaceId = g.Key!, Similarity = g.Max(x => x.Similarity ?? 0) })
                .ToList();
        }

        private async Task<List<Case>> LoadCandidateCasesAsync(IReadOnlyCollection<FaceMatchResult> faceMatches)
        {
            var faceIds = faceMatches.Select(x => x.FaceId).ToList();

            if (faceIds.Count == 0)
                return [];

            return await _unitOfWork.Repository<Case>()
                .Query(tracked: false, includes: [c => c.CaseFiles, c => c.User])
                .Where(c =>
                (c.Status == CaseStatus.Pending || c.Status == CaseStatus.Active)
                &&
                c.CaseFiles.Any(f => f.FaceId != null && faceIds.Contains(f.FaceId))).ToListAsync();
        }

        private List<MatchedCaseDto> FilterMatchedCases(IReadOnlyCollection<Case> candidateCases, IReadOnlyCollection<FaceMatchResult> faceMatches)
        {
            var matchedCases = new List<MatchedCaseDto>();

            foreach (var candidate in candidateCases)
            {
                CaseFile? matchedPhoto = null;
                FaceMatchResult? matchedFace = null;

                foreach (var photo in candidate.CaseFiles)
                {
                    matchedFace = faceMatches.FirstOrDefault(x => x.FaceId == photo.FaceId);

                    if (matchedFace != null)
                    {
                        matchedPhoto = photo;
                        break;
                    }
                }

                if (matchedPhoto == null || matchedFace == null)
                    continue;

                var similarity = matchedFace.Similarity ?? 0;

                if (!PassesVerification(similarity))
                    continue;

                var dto = _mapper.Map<MatchedCaseDto>(candidate);
                dto.Similarity = similarity;
                dto.MainPhoto = matchedPhoto.ImagePath;

                matchedCases.Add(dto);
            }

            return matchedCases;
        }


        private static bool PassesVerification(float similarity)
        {
            return similarity >= MinimumSimilarity;
        }
    }
}
