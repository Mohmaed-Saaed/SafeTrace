using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.Application.Services.Cases
{
    public class UnKnownCaseService : IUnknownCaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICaseHelperService _caseHelper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UnKnownCaseService> _logger;

        public UnKnownCaseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
            UserManager<ApplicationUser> userManager,
            ILogger<UnKnownCaseService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _caseHelper = caseHelper;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto, string userId)
        {
            _logger.LogInformation("Start creating unknown case for UserId: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User not found. UserId: {UserId}", userId);
                throw new NotFoundException("User was not found.");
            }

            if (user.VerificationStatus != VerificationStatus.Verified)
            {
                _logger.LogWarning("User is not verified. UserId: {UserId}", userId);
                throw new UnauthorizedException("You must verify your account before creating a case.");
            }

            _logger.LogInformation("User validated successfully. UserId: {UserId}", userId);

            var unknownCase = _mapper.Map<UnknownCase>(dto);

            unknownCase.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(unknownCase.Age);
            unknownCase.UserId = userId;
            unknownCase.CreatedAt = DateTime.UtcNow;
            unknownCase.Status = CaseStatus.Pending;
            unknownCase.CaseType = CaseType.Unknown;
            unknownCase.CaseCode = await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.UNK);

            _logger.LogInformation("Unknown case object created in memory. CaseCode: {CaseCode}", unknownCase.CaseCode);

            if (dto.Photos != null && dto.Photos.Any())
            {
                _logger.LogInformation("Uploading {Count} photos for CaseCode: {CaseCode}", dto.Photos.Count(), unknownCase.CaseCode);

                unknownCase.Photos = await _caseHelper.HandlePhotoUploadsAsync(dto.Photos, "UnknownCases");
                _caseHelper.EnsureSinglePrimaryPhoto(unknownCase.Photos);

                _logger.LogInformation("Photos uploaded successfully for CaseCode: {CaseCode}", unknownCase.CaseCode);
            }

            await _unitOfWork.Repository<UnknownCase>().CreateAsync(unknownCase);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Unknown case saved successfully. CaseCode: {CaseCode}", unknownCase.CaseCode);

            return ApiResponse<string>.Ok(message: "تم إنشاء حالة مجهول الهوية بنجاح");
        }

        public async Task<ApiResponse<string>> UpdateUnknownCaseAsync(
            long id,
            UpdateUnknownCaseDto dto,
            string userId)
        {
            _logger.LogInformation(
                "Starting update for UnknownCase. CaseId: {CaseId}, UserId: {UserId}",
                id, userId);

            var unknownCase = await _caseHelper.GetValidCaseAsync<UnknownCase>(id, userId, checkOwnership: true, includes: new System.Linq.Expressions.Expression<Func<UnknownCase, object>>[] { x => x.Photos });
            _caseHelper.ValidateCaseIsEditable(unknownCase);

            if (unknownCase.Status == CaseStatus.Active)
            {
                _logger.LogInformation(
                    "Case status changed from Active to Pending. CaseId: {CaseId}",
                    id);

                unknownCase.Status = CaseStatus.Pending;
            }

            var currentPhotosCount = unknownCase.Photos.Count;
            var deletedCount = dto.DeletedPhotoIds?.Count ?? 0;
            var addedCount = dto.NewPhotos?.Count ?? 0;

            _logger.LogInformation(
                "Photos update for CaseId: {CaseId}. Current: {Current}, ToDelete: {Deleted}, ToAdd: {Added}",
                id, currentPhotosCount, deletedCount, addedCount);

            var expectedCount = currentPhotosCount - deletedCount + addedCount;

            if (expectedCount <= 0)
            {
                _logger.LogWarning(
                    "Update rejected because it would leave the case without photos. CaseId: {CaseId}",
                    id);

                throw new BadRequestException(
                    "يجب ان تضع صوره واحده علي الاقل وان تم حذف جميع الصوره يجب استبدال اول صوره علي الاقل ");
            }

            _mapper.Map(dto, unknownCase);
            unknownCase.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(unknownCase.Age);

            var filesToDelete = new List<string>();

            if (dto.DeletedPhotoIds != null)
            {
                var toRemove = unknownCase.Photos.Where(p => dto.DeletedPhotoIds.Contains(p.Id)).ToList();
                foreach (var photo in toRemove)
                {
                    _logger.LogInformation("Deleting photo. PhotoId: {PhotoId}, CaseId: {CaseId}", photo.Id, id);
                    filesToDelete.Add(photo.ImagePath);
                    unknownCase.Photos.Remove(photo);
                }
            }

            if (dto.NewPhotos != null)
            {
                _logger.LogInformation("Adding {Count} new photos to CaseId: {CaseId}", dto.NewPhotos.Count, id);
                var newUploadedPhotos = await _caseHelper.HandlePhotoUploadsAsync(dto.NewPhotos, "UnknownCases", id);
                foreach (var photo in newUploadedPhotos)
                {
                    unknownCase.Photos.Add(photo);
                }
            }

            _caseHelper.EnsureSinglePrimaryPhoto(unknownCase.Photos);

            unknownCase.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            await _caseHelper.CleanupPhysicalFilesAsync(filesToDelete);

            _logger.LogInformation(
                "Unknown case updated successfully. CaseId: {CaseId}, UserId: {UserId}",
                id, userId);

            return ApiResponse<string>.Ok(message: "تم تحديث حالة مجهول الهوية بنجاح");
        }
    }
}