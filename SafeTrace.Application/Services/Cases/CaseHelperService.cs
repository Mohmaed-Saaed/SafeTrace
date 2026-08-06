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
        private const int MaxAgeDifference = 5;

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

        public async Task<List<CaseFile>> CreateCaseFilesAsync(IFormFile primaryImage, IEnumerable<IFormFile>? additionalImages, IFormFile? video, string folderName, long caseId = 0)
        {
            var files = new List<CaseFile>
            {
                await CreateCaseFileAsync(primaryImage, folderName, caseId, true)
            };

            if (additionalImages != null)
            {
                foreach (var image in additionalImages)
                {
                    files.Add(await CreateCaseFileAsync(image, folderName, caseId, false));
                }
            }

            if (video != null)
            {
                files.Add(await CreateCaseFileAsync(video, folderName, caseId, false));
            }

            return files;
        }

        private async Task<CaseFile> CreateCaseFileAsync(IFormFile file, string folderName, long caseId, bool isPrimary)
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
                    _logger.LogWarning(ex, "Failed to index face for file {FileName}.", file.FileName);
                }
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

        public void SetPrimaryImage(ICollection<CaseFile> files, long primaryPhotoId)
        {
            var images = files
                .Where(f => !VideoExtensions.Contains(Path.GetExtension(f.ImagePath)))
                .ToList();

            if (!images.Any())
                throw new BadRequestException("الحالة لا تحتوي على صور.");

            if (!images.Any(i => i.Id == primaryPhotoId))
                throw new BadRequestException("الصورة الأساسية المحددة غير موجودة.");

            foreach (var image in images)
            {
                image.IsPrimary = image.Id == primaryPhotoId;
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
            try
            {
                await _faceRecognitionService.DeleteFacesAsync(faceIds?.ToList() ?? []);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete faces for case {CaseId}", caseId);
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
            var isUpdate = existingFaceIds != null && existingFaceIds.Any();
            var hasAdditional = additionalImages != null && additionalImages.Any();

            if (!isUpdate)
            {
                if (primaryImage != null && hasAdditional)
                {
                    foreach (var additional in additionalImages!)
                    {
                        var comparison = await _faceRecognitionService.CompareFacesAsync(primaryImage, additional);
                        if (!comparison.Success || !comparison.IsSamePerson)
                        {
                            throw new BadRequestException("جميع الصور المرفقة يجب أن تكون لنفس الشخص.");
                        }
                    }
                }
            }
            else
            {
                if (primaryImage != null)
                {
                    var matches = await _faceRecognitionService.SearchByImageAsync(primaryImage);
                    var isSamePerson = matches.Any(m => existingFaceIds!.Contains(m.FaceId) && PassesVerification(m.Similarity ?? 0));
                    if (!isSamePerson)
                    {
                        throw new BadRequestException("الصورة الجديدة لا تبدو لنفس الشخص الموجود في هذا البلاغ.\n\nإذا كان هذا شخصًا آخر، يرجى إنشاء بلاغ جديد بدلاً من تعديل البلاغ الحالي.");
                    }
                    
                    if (hasAdditional)
                    {
                        foreach (var additional in additionalImages!)
                        {
                            var comparison = await _faceRecognitionService.CompareFacesAsync(primaryImage, additional);
                            if (!comparison.Success || !comparison.IsSamePerson)
                            {
                                throw new BadRequestException("جميع الصور داخل البلاغ يجب أن تكون لنفس الشخص.");
                            }
                        }
                    }
                }
                else if (hasAdditional)
                {
                    foreach (var additional in additionalImages!)
                    {
                        var matches = await _faceRecognitionService.SearchByImageAsync(additional);
                        var isSamePerson = matches.Any(m => existingFaceIds!.Contains(m.FaceId) && PassesVerification(m.Similarity ?? 0));
                        if (!isSamePerson)
                        {
                            throw new BadRequestException("جميع الصور داخل البلاغ يجب أن تكون لنفس الشخص.");
                        }
                    }
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
