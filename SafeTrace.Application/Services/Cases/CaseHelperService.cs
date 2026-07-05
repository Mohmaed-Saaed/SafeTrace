using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.Application.Services.Cases
{
    public class CaseHelperService : ICaseHelperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly ILogger<CaseHelperService> _logger;

        public CaseHelperService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IFaceRecognitionService faceRecognitionService,
            ILogger<CaseHelperService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _faceRecognitionService = faceRecognitionService;
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
    }
}