using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.DTOs.UnKnownCase.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.Application.Services.Cases
{
    public class UnKnownCaseService : BaseCasesService<UnknownCase, UnknownCaseListDto, UnknownCaseDetailDto, UnknownCasesFilterDto>, IUnknownCaseService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UnKnownCaseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
            UserManager<ApplicationUser> userManager,
            ILogger<UnKnownCaseService> logger)
            : base(unitOfWork, mapper, caseHelper, logger)
        {
            _userManager = userManager;
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
            unknownCase.Street ??= string.Empty;

            _logger.LogInformation(
                "Unknown case object created in memory. CaseCode: {CaseCode}",
                unknownCase.CaseCode);

            var uploadedFiles = new List<CaseFile>();

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    uploadedFiles = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        "UnknownCase");

                    unknownCase.CaseFiles = uploadedFiles;

                    await _unitOfWork.Repository<UnknownCase>().CreateAsync(unknownCase);

                    return true;
                },
                onFailureAsync: ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(
                        uploadedFiles.Select(p => p.ImagePath));

                    _logger.LogError(
                        ex,
                        "Failed to create unknown case for user {UserId}",
                        userId);

                    return Task.CompletedTask;
                });

            _logger.LogInformation(
                "Unknown case {CaseCode} created successfully by user {UserId}.",
                unknownCase.CaseCode,
                userId);

            return ApiResponse<string>.Ok(message: "تم إنشاء حالة مجهول الهوية بنجاح");
        }

        public async Task<ApiResponse<string>> UpdateUnknownCaseAsync(long id, UpdateUnknownCaseDto dto, string userId)
        {
            _logger.LogInformation("Starting update for UnknownCase. CaseId: {CaseId}, UserId: {UserId}", id, userId);

            var unknownCase = await _caseHelper.GetValidCaseAsync<UnknownCase>(
                id,
                userId,
                checkOwnership: true,
                includes: [x => x.CaseFiles]);

            _caseHelper.ValidateCaseIsEditable(unknownCase);

            if (unknownCase.Status == CaseStatus.Active)
            {
                _logger.LogInformation("Case status changed from Active to Pending. CaseId: {CaseId}", id);
                unknownCase.Status = CaseStatus.Pending;
            }

            var currentPhotosCount = unknownCase.CaseFiles.Count;
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

            unknownCase.Gender = dto.Gender;
            if (dto.FName is not null) unknownCase.FName = dto.FName;
            if (dto.SName is not null) unknownCase.SName = dto.SName;
            if (dto.TName is not null) unknownCase.TName = dto.TName;
            if (dto.LName is not null) unknownCase.LName = dto.LName;

            unknownCase.Age = dto.Age;
            unknownCase.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(unknownCase.Age);

            unknownCase.Government = dto.Government;
            unknownCase.City = dto.City;

            if (dto.Street is not null)
                unknownCase.Street = dto.Street;

            if (dto.CommunicationPhone is not null)
                unknownCase.CommunicationPhone = dto.CommunicationPhone;

            if (dto.Description is not null)
                unknownCase.Description = dto.Description;

            var uploadedPhotos = new List<CaseFile>();
            var filesToDelete = new List<string>();

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    if (dto.DeletedPhotoIds?.Any() == true)
                    {
                        var toRemove = unknownCase.CaseFiles
                            .Where(p => dto.DeletedPhotoIds.Contains(p.Id))
                            .ToList();

                        filesToDelete.AddRange(toRemove.Select(p => p.ImagePath));

                        foreach (var photo in toRemove)
                        {
                            _logger.LogInformation(
                                "Deleting photo. PhotoId: {PhotoId}, CaseId: {CaseId}",
                                photo.Id,
                                id);

                            unknownCase.CaseFiles.Remove(photo);
                        }
                    }

                    if (dto.NewPhotos?.Any() == true)
                    {
                        _logger.LogInformation(
                            "Adding {Count} new photos to CaseId: {CaseId}",
                            dto.NewPhotos.Count,
                            id);

                        uploadedPhotos = await _caseHelper.CreateCaseFilesAsync(
                            dto.NewPhotos.First(),
                            dto.NewPhotos.Skip(1),
                            null,
                            "UnknownCase",
                            unknownCase.Id);

                        foreach (var photo in uploadedPhotos)
                            unknownCase.CaseFiles.Add(photo);
                    }

                    if (dto.PrimaryPhotoId.HasValue)
                    {
                        _caseHelper.SetPrimaryImage(
                            unknownCase.CaseFiles,
                            dto.PrimaryPhotoId.Value);
                    }

                    unknownCase.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<UnknownCase>().Update(unknownCase);

                    return true;
                },
                onFailureAsync: ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(
                        uploadedPhotos.Select(p => p.ImagePath));

                    _logger.LogError(
                        ex,
                        "Failed to update unknown case {CaseId} for user {UserId}",
                        id,
                        userId);

                    return Task.CompletedTask;
                });

            _caseHelper.CleanupPhysicalFiles(filesToDelete);

            _logger.LogInformation(
                "Unknown case updated successfully. CaseId: {CaseId}, UserId: {UserId}",
                id,
                userId);

            return ApiResponse<string>.Ok(message: "تم تحديث حالة مجهول الهوية بنجاح");
        }
    
    }
}