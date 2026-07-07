using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.Application.Services.Cases
{
    public class CaseHelperService : ICaseHelperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IMapper _mapper;
        private readonly ILogger<CaseHelperService> _logger;

        private const float MinimumSimilarity = 80f;
        private const int MaxAgeDifference = 5;

        public CaseHelperService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IFaceRecognitionService faceRecognitionService,
            IMapper mapper,
            ILogger<CaseHelperService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _faceRecognitionService = faceRecognitionService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a case by ID with optional ownership and status validation.
        /// </summary>
        public async Task<TEntity> GetValidCaseAsync<TEntity>(
            long id,
            string? userId = null,
            bool checkOwnership = false,
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

            if (checkOwnership && !string.IsNullOrEmpty(userId) && entity.UserId != userId)
            {
                throw new UnauthorizedException("لا يمكن تنفيذ هذا الإجراء على هذه الحالة.");
            }

            return entity;
        }

        /// <summary>
        /// Validates that a case can be modified (not Found, Expired, or Deleted).
        /// </summary>
        public void ValidateCaseIsEditable(Case entity)
        {
            if (entity.Status == CaseStatus.Found || entity.Status == CaseStatus.Expired || entity.Status == CaseStatus.Deleted)
            {
                throw new BadRequestException($"لا يمكن تنفيذ هذا الإجراء على حالة بحالة '{entity.Status}'.");
            }
        }
        
        /// <summary>
        /// Ensures the user exists and has a verified account.
        /// </summary>
        public async Task ValidateVerifiedUserAsync(string userId)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>()
                .GetOneAsync(u => u.Id == userId, tracked: false);

            if (user is null)
                throw new NotFoundException("المستخدم غير موجود.");

            if (user.VerificationStatus != VerificationStatus.Verified)
                throw new UnauthorizedException("يجب توثيق حسابك قبل تنفيذ هذا الإجراء.");
        }

        /// <summary>
        /// Resolves the age category ID for a given age.
        /// </summary>
        public async Task<int> ResolveAgeCategoryIdAsync(int age)
        {
            var category = await _unitOfWork.Repository<AgeCategory>()
                .GetOneAsync(c => age >= c.MinAge && age <= c.MaxAge, tracked: false);

            if (category is null)
                throw new NotFoundException($"لا يوجد فئة عمرية محددة للعمر {age}.");

            return category.Id;
        }

        /// <summary>
        /// Generates a unique case code with the specified prefix.
        /// </summary>
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

        /// <summary>
        /// Creates case file entities from uploaded files.
        /// </summary>
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

            if (!VideoExtensions.Contains(Path.GetExtension(file.FileName)))
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
                CreatedAt = DateTime.UtcNow
            };
        }
       
        /// <summary>
        /// Sets the primary image for a case.
        /// </summary>
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

        /// <summary>
        /// Deletes physical files from storage with error logging.
        /// </summary>
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

        /// <summary>
        /// Deletes face records from the recognition service.
        /// </summary>
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
    
        /// <summary>
        /// Finds existing cases that match the provided subject and face image.
        /// </summary>
        public async Task<MatchedCasesResult> FindMatchedCasesAsync(CaseMatchSubjectInfoDto subject, IFormFile primaryImage)
        {
            var faceMatches = await SearchFacesAsync(primaryImage);
            if (faceMatches.Count == 0)
                return MatchedCasesResult.Empty;

            var candidateCases = await LoadCandidateCasesAsync(faceMatches);
            if (candidateCases.Count == 0)
                return MatchedCasesResult.Empty;

            var matchedCases = FilterMatchedCases(candidateCases, faceMatches, subject);

            return new MatchedCasesResult
            {
                HasMatched = matchedCases.Count != 0,
                DuplicateCases = matchedCases
            };
        }

        private async Task<List<FaceMatchResult>> SearchFacesAsync(IFormFile primaryImage)
        {
            var faceMatches = await _faceRecognitionService.SearchByImageAsync(primaryImage);

            if (faceMatches == null || faceMatches.Count == 0)
                return [];

            return faceMatches
                .Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                .GroupBy(x => x.FaceId)
                .Select(g => new FaceMatchResult{FaceId = g.Key!, Similarity = g.Max(x => x.Similarity ?? 0)})
                .ToList();
        }
 
        private async Task<List<Case>> LoadCandidateCasesAsync(IReadOnlyCollection<FaceMatchResult> faceMatches)
        {
            var faceIds = faceMatches.Select(x => x.FaceId).ToList();

            if (faceIds.Count == 0)
                return [];

            return await _unitOfWork.Repository<Case>()
                .Query(tracked: false, includes: [c => c.CaseFiles, c => c.User])
                .Where(c => c.Status == CaseStatus.Active && c.CaseFiles.Any(f => f.FaceId != null && faceIds.Contains(f.FaceId)))
                .ToListAsync();
        }
 
        private List<MatchedCaseDto> FilterMatchedCases(IReadOnlyCollection<Case> candidateCases, IReadOnlyCollection<FaceMatchResult> faceMatches, CaseMatchSubjectInfoDto subject)
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

                if (!PassesVerification(candidate, similarity, subject))
                    continue;

                var dto = _mapper.Map<MatchedCaseDto>(candidate);
                dto.Similarity = similarity;
                dto.MainPhotoPath = matchedPhoto.ImagePath;

                matchedCases.Add(dto);
            }

            return matchedCases;
        }
        
        private static bool PassesVerification(Case candidate, float similarity, CaseMatchSubjectInfoDto subject)
        {
            if (similarity < MinimumSimilarity)
                return false;

            if (candidate.Gender != subject.Gender)
                return false;

            if (Math.Abs(candidate.Age - subject.Age) > MaxAgeDifference)
                return false;

            return true;
        }
    
    }
}