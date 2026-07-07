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

        // Tunable thresholds for face-match verification. Kept as constants here (not
        // config) for now — promote to IOptions<T> later if they need to be tuned
        // without a redeploy.
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

        // VALIDATION / LOOKUP
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
                _logger.LogWarning("Case {CaseId} not found or deleted.", id);
                throw new NotFoundException($"Case {id} not found.");
            }

            if (checkOwnership && !string.IsNullOrEmpty(userId) && entity.UserId != userId)
            {
                _logger.LogWarning("Unauthorized attempt to access Case {CaseId} by User {UserId}", id, userId);
                throw new UnauthorizedException("You are not authorized to perform this action.");
            }

            return entity;
        }

        public void ValidateCaseIsEditable(Case entity)
        {
            if (entity.Status == CaseStatus.Found || entity.Status == CaseStatus.Expired || entity.Status == CaseStatus.Deleted)
            {
                _logger.LogWarning("Attempt to edit/update uneditable Case {CaseId} with status {Status}", entity.Id, entity.Status);
                throw new BadRequestException($"Cannot perform this action on a case with status '{entity.Status}'.");
            }
        }
        
        public async Task<int> ResolveAgeCategoryIdAsync(int age)
        {
            var category = await _unitOfWork.Repository<AgeCategory>()
                .GetOneAsync(c => age >= c.MinAge && age <= c.MaxAge, tracked: false);

            if (category is null)
                throw new NotFoundException($"No age category configured for age {age}.");

            return category.Id;
        }

        public async Task<string> GenerateCaseCodeAsync(CaseCodePrefix prefix)
        {
            if (!SequenceNames.TryGetValue(prefix, out var sequenceName))
                throw new ArgumentOutOfRangeException(nameof(prefix));

            var number = await _unitOfWork.GetNextSequenceValueAsync(sequenceName);

            return $"{prefix}-{number}";
        }
        
        private static readonly Dictionary<CaseCodePrefix, string> SequenceNames = new()
        {
            { CaseCodePrefix.LNG, "LongTermCaseSequence" },
            { CaseCodePrefix.URG, "UrgentCaseSequence" },
            { CaseCodePrefix.UNK, "UnknownCaseSequence" }
        };

        // PHOTO HELPERS
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
       
        public void SetPrimaryImage(ICollection<CaseFile> files, long primaryPhotoId)
        {
            var images = files
                .Where(f => !VideoExtensions.Contains(Path.GetExtension(f.ImagePath)))
                .ToList();

            if (!images.Any())
                throw new BadRequestException("The case has no images.");

            if (!images.Any(i => i.Id == primaryPhotoId))
                throw new BadRequestException("The specified primary image does not exist.");

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

        // FACE RECOGNITION HELPERS
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
    
        // =========================================================
        // FACE MATCHING
        // Finds existing cases that may belong to the same person.
        // It only returns matched cases; each case service decides
        // how to handle them (block, show, or continue creating).
        // =========================================================
        public async Task<MatchedCasesResult> FindMatchedCasesAsync(CaseMatchSubjectInfoDto subject, IFormFile primaryImage)
        {
            // Step 1: Search similar faces in AWS / Vector DB
            var faceMatches = await SearchFacesAsync(primaryImage);
            if (faceMatches.Count == 0)
                return MatchedCasesResult.Empty;

            // Step 2: Load matching cases
            var candidateCases = await LoadCandidateCasesAsync(faceMatches);
            if (candidateCases.Count == 0)
                return MatchedCasesResult.Empty;


            // Step 3: Verify and filter matches
            var matchedCases = FilterMatchedCases(candidateCases, faceMatches, subject);

            return new MatchedCasesResult
            {
                HasMatched = matchedCases.Count != 0,
                DuplicateCases = matchedCases
            };
        }

        /// <summary>
        /// Searches the face recognition service for similar faces.
        /// </summary>
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
 
        /// <summary>
        /// Loads active cases that contain the matched FaceIds.
        /// </summary>
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
 
        /// <summary>
        /// Filters candidate cases using business verification rules.
        /// </summary>
        private List<MatchedCaseDto> FilterMatchedCases(IReadOnlyCollection<Case> candidateCases, IReadOnlyCollection<FaceMatchResult> faceMatches, CaseMatchSubjectInfoDto subject)
        {
            var matchedCases = new List<MatchedCaseDto>();

            foreach (var candidate in candidateCases)
            {
                CaseFile? matchedPhoto = null;
                FaceMatchResult? matchedFace = null;

                // Find the first candidate photo returned by AWS
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

                // Verify business information
                if (!PassesVerification(candidate, similarity, subject))
                    continue;

                var dto = _mapper.Map<MatchedCaseDto>(candidate);
                dto.Similarity = similarity;
                dto.MainPhotoPath = matchedPhoto.ImagePath;

                matchedCases.Add(dto);
            }

            return matchedCases;
        }
        
        /// <summary>
        /// Verifies whether the candidate is likely the same person.
        /// </summary>
        private static bool PassesVerification(Case candidate, float similarity, CaseMatchSubjectInfoDto subject)
        {
            // Similarity
            if (similarity < MinimumSimilarity)
                return false;

            // Gender
            if (candidate.Gender != subject.Gender)
                return false;

            // Age
            if (Math.Abs(candidate.Age - subject.Age) > MaxAgeDifference)
                return false;

            return true;
        }
    
    }
}