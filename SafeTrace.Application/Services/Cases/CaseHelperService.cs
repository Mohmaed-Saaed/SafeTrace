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
        // use task Lock
        public async Task<string> GenerateCaseCodeAsync(CaseCodePrefix prefix)
        {

            var repository = _unitOfWork.Repository<Case>();
            
            var lastCase = await repository.Query(tracked: false)
                .Where(x => x.CaseCode.StartsWith(prefix + "-"))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (lastCase == null || string.IsNullOrWhiteSpace(lastCase.CaseCode))
            {
                return $"{prefix}-1000";
            }

            var numberPart = lastCase.CaseCode.Split('-').Last();

            var number = int.Parse(numberPart);

            return $"{prefix}-{number + 1}";
        }
        
        // PHOTO HELPERS // Handel More Files
        public async Task<List<CaseFile>> HandlePhotoUploadsAsync(IEnumerable<IFormFile> files, string folderName, long caseId = 0)
        {
            var uploadedPhotos = new List<CaseFile>();

            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    var imagePath = await _fileStorageService.SaveFileAsync(file, folderName);
                    uploadedPhotos.Add(new CaseFile
                    {
                        CaseId = caseId,
                        ImagePath = imagePath,
                        CreatedAt = DateTime.UtcNow,
                        IsPrimary = false
                    });
                }
            }

            return uploadedPhotos;
        }

        public void EnsureSinglePrimaryPhoto(ICollection<CaseFile> photos, long? preferredPrimaryId = null)
        {
            if (photos == null || !photos.Any())
                return;

            if (preferredPrimaryId.HasValue)
            {
                var targetPhoto = photos.FirstOrDefault(p => p.Id == preferredPrimaryId.Value);
                if (targetPhoto == null)
                    throw new BadRequestException("The specified primary photo does not exist.");

                foreach (var p in photos)
                {
                    p.IsPrimary = p.Id == preferredPrimaryId.Value;
                }

                return;
            }

            var primaries = photos.Where(p => p.IsPrimary).ToList();
            if (primaries.Count == 0)
            {
                photos.First().IsPrimary = true;
            }
            else if (primaries.Count > 1)
            {
                foreach (var p in primaries.Skip(1))
                    p.IsPrimary = false;
            }
        }

        public async Task CleanupPhysicalFilesAsync(IEnumerable<string> filePaths)
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

            await Task.CompletedTask;
        }

        // FACE RECOGNITION HELPERS
        public async Task DeleteFacesAsync(IEnumerable<string> faceIds, long caseIdForLogging)
        {
            var ids = faceIds?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();
            if (ids.Count == 0)
                return;

            try
            {
                await _faceRecognitionService.DeleteFacesAsync(ids);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete {Count} face(s) from AWS for Case {CaseId}.", ids.Count, caseIdForLogging);
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