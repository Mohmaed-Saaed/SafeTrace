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
        private const string FolderName = "UnknownCase";
        
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

        /// <summary>
        /// Creates a new unknown case with pending status.
        /// </summary>
        public async Task<ApiResponse<string>> CreateUnknownCaseAsync(string userId, CreateUnknownDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("لم يتم العثور على المستخدم.");
            }

            if (user.VerificationStatus != VerificationStatus.Verified)
            {
                throw new UnauthorizedException("يجب عليك التحقق من حسابك قبل إنشاء طلب دعم.");
            }

            var entity = _mapper.Map<UnknownCase>(dto);

            entity.UserId = userId;
            entity.Status = CaseStatus.Pending;
            entity.CaseType = CaseType.Unknown;
            entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.UNK);
            entity.CreatedAt = DateTime.UtcNow;

            var uploadedFiles = new List<CaseFile>();

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    uploadedFiles = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        FolderName);

                    entity.CaseFiles = uploadedFiles;

                    await _unitOfWork.Repository<UnknownCase>().CreateAsync(entity);

                    return true;
                },
                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(uploadedFiles.Select(p => p.ImagePath));
                    await _caseHelper.DeleteFacesAsync(entity.CaseFiles.Select(x => x.FaceId), entity.Id);

                    _logger.LogError(
                        ex,
                        "Failed to create unknown case for user {UserId}",
                        userId);
                });

            _logger.LogInformation(
                "Unknown case {CaseCode} created successfully by user {UserId}.",
                entity.CaseCode,
                userId);

            return ApiResponse<string>.Ok(message: "تم إنشاء حالة مجهول الهوية بنجاح");
        }

        /// <summary>
        /// Updates an existing unknown case with photo management.
        /// </summary>
        public async Task<ApiResponse<string>> UpdateUnknownCaseAsync(long id, string userId, UpdateUnknownCaseDto dto)
        {
            var entity = await _caseHelper.GetValidCaseAsync<UnknownCase>(
                id,
                userId,
                checkOwnership: true,
                includes: [x => x.CaseFiles]);

            _caseHelper.ValidateCaseIsEditable(entity);

            if (entity.Status == CaseStatus.Active)
            {
                entity.PreviousStatus = entity.Status;
                entity.Status = CaseStatus.Pending;
            }

            var currentPhotosCount = entity.CaseFiles.Count;
            var deletedCount = dto.DeletedPhotoIds?.Count ?? 0;
            var addedCount = dto.NewPhotos?.Count ?? 0;

            if (currentPhotosCount - deletedCount + addedCount <= 0)
            {
                throw new BadRequestException(
                    "يجب أن تحتوي الحالة على صورة واحدة على الأقل.");
            }

            entity.Gender = dto.Gender;

            if (dto.FName is not null)
                entity.FName = dto.FName;

            if (dto.SName is not null)
                entity.SName = dto.SName;

            if (dto.TName is not null)
                entity.TName = dto.TName;

            if (dto.LName is not null)
                entity.LName = dto.LName;

            entity.Age = dto.Age;
            entity.AgeCategoryId =
                await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

            entity.Government = dto.Government;
            entity.City = dto.City;

            if (dto.Street is not null)
                entity.Street = dto.Street;

            if (dto.CommunicationPhone is not null)
                entity.CommunicationPhone = dto.CommunicationPhone;

            if (dto.Description is not null)
                entity.Description = dto.Description;

            var uploadedPhotos = new List<CaseFile>();
            var filesToDelete = new List<string>();
            var faceIdsToDelete = new List<string>();

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    if (dto.DeletedPhotoIds?.Count > 0)
                    {
                        var toRemove = entity.CaseFiles
                            .Where(p => dto.DeletedPhotoIds.Contains(p.Id))
                            .ToList();

                        filesToDelete.AddRange(toRemove.Select(p => p.ImagePath));

                        faceIdsToDelete.AddRange(
                            toRemove
                                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                                .Select(p => p.FaceId!));

                        foreach (var photo in toRemove)
                            entity.CaseFiles.Remove(photo);
                    }

                    if (dto.NewPhotos?.Count > 0)
                    {
                        uploadedPhotos = await _caseHelper.CreateCaseFilesAsync(
                            dto.NewPhotos.First(),
                            dto.NewPhotos.Skip(1),
                            null,
                            FolderName,
                            entity.Id);

                        foreach (var photo in uploadedPhotos)
                            entity.CaseFiles.Add(photo);
                    }

                    if (dto.PrimaryPhotoId.HasValue)
                    {
                        _caseHelper.SetPrimaryImage(
                            entity.CaseFiles,
                            dto.PrimaryPhotoId.Value);
                    }

                    entity.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<UnknownCase>().Update(entity);

                    return true;
                },
                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(
                        uploadedPhotos.Select(x => x.ImagePath));

                    await _caseHelper.DeleteFacesAsync(
                        uploadedPhotos
                            .Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                            .Select(x => x.FaceId!),
                        entity.Id);

                    _logger.LogError(
                        ex,
                        "Failed to update unknown case {CaseId} for user {UserId}",
                        id,
                        userId);
                });

            _caseHelper.CleanupPhysicalFiles(filesToDelete);

            if (faceIdsToDelete.Count > 0)
            {
                await _caseHelper.DeleteFacesAsync(
                    faceIdsToDelete,
                    entity.Id);
            }

            _logger.LogInformation(
                "Unknown case {CaseId} updated successfully.",
                entity.Id);

            return ApiResponse<string>.Ok(message: "تم تحديث حالة مجهول الهوية بنجاح");
        }
            
    }
}