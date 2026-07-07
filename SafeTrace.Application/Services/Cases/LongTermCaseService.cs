using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.LongTermCase.Response;
using SafeTrace.Application.DTOs.LongTermCase.Request;


namespace SafeTrace.Application.Services.Cases
{
    public class LongTermCaseService : BaseCasesService<LongTermMissingCase, LongTermCaseListDto, LongTermCaseDetailDto, LongTermCaseFilterDto>, ILongTermCaseService
    {
        private readonly IFileStorageService _fileStorageService;

        public LongTermCaseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorage,
            ICaseHelperService caseHelper,
            ILogger<LongTermCaseService> logger)
            : base(unitOfWork, mapper, caseHelper, logger)
        {
            _fileStorageService = fileStorage;
        }

        protected override void DeleteAdditionalFiles(LongTermMissingCase entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.PoliceReportImage))
            {
                _caseHelper.CleanupPhysicalFiles([entity.PoliceReportImage]);
            }
        }
        
        public async Task<ApiResponse<string>> CreateAsync(CreateLongTermCaseDto dto, string userId)
        {
            var entity = _mapper.Map<LongTermMissingCase>(dto);

            entity.UserId = userId;
            entity.CaseType = CaseType.LongTerm;
            entity.Status = CaseStatus.Pending;
            entity.CreatedAt = DateTime.UtcNow;
            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.LNG);
            entity.Street ??= string.Empty;
            entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

            if (dto.PoliceReportImage is not null)
            {
                entity.PoliceReportImage = await _fileStorageService.SaveFileAsync(dto.PoliceReportImage, "LongTermCase/PoliceReports");
            }

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    var uploadedPhotos = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        "LongTermCase",
                        entity.Id);

                    foreach (var photo in uploadedPhotos)
                        entity.CaseFiles.Add(photo);

                    await _unitOfWork.Repository<LongTermMissingCase>().CreateAsync(entity);

                    return true;
                },
                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(entity.CaseFiles.Select(x => x.ImagePath));
                    _fileStorageService.DeleteFile(entity.PoliceReportImage);
                    await _caseHelper.DeleteFacesAsync(entity.CaseFiles.Select(x => x.FaceId), entity.Id);
                });

            _logger.LogInformation(
                "Created long-term missing case. CaseId={CaseId}, CaseCode={CaseCode}, UserId={UserId}",
                entity.Id, entity.CaseCode, userId);

            return ApiResponse<string>.Ok(message: "LongTerm case created successfully.");
        }

        public async Task<ApiResponse<string>> UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId)
        {
            var entity = await _caseHelper.GetValidCaseAsync<LongTermMissingCase>(
                id,
                userId,
                checkOwnership: true,
                includes: [c => c.CaseFiles]);

            _caseHelper.ValidateCaseIsEditable(entity);

            var uploadedPhotos = new List<CaseFile>();
            var filesToDelete = new List<string>();
            var faceIdsToDelete = new List<string>();

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    entity.Age = dto.Age;
                    entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

                    entity.Gender = dto.Gender;
                    entity.FName = dto.FName;
                    entity.SName = dto.SName;
                    if (dto.TName is not null) entity.TName = dto.TName;
                    if (dto.LName is not null) entity.LName = dto.LName;
                    if (dto.Relation.HasValue) entity.Relation = dto.Relation.Value;
                    if (dto.Description is not null) entity.Description = dto.Description;
                    if (dto.Government is not null) entity.Government = dto.Government;
                    if (dto.City is not null) entity.City = dto.City;
                    if (dto.Street is not null) entity.Street = dto.Street;

                    if (dto.PoliceReportImage is not null)
                    {
                        _fileStorageService.DeleteFile(entity.PoliceReportImage);

                        entity.PoliceReportImage = await _fileStorageService.SaveFileAsync(
                                dto.PoliceReportImage,
                                "LongTermCase/PoliceReports");
                    }

                    if (dto.DeletedPhotoIds is { Count: > 0 })
                    {
                        var toRemove = entity.CaseFiles
                            .Where(p => dto.DeletedPhotoIds.Contains(p.Id))
                            .ToList();

                        foreach (var photo in toRemove)
                        {
                            filesToDelete.Add(photo.ImagePath);

                            if (!string.IsNullOrWhiteSpace(photo.FaceId))
                                faceIdsToDelete.Add(photo.FaceId);

                            entity.CaseFiles.Remove(photo);
                        }
                    }

                    if (dto.NewPhotos is not null && dto.NewPhotos.Any())
                    {
                        var newPhotos = await _caseHelper.CreateCaseFilesAsync(
                            dto.NewPhotos.First(),
                            dto.NewPhotos.Skip(1),
                            null,
                            "LongTermCase",
                            entity.Id);

                        uploadedPhotos.AddRange(newPhotos);

                        foreach (var photo in newPhotos)
                            entity.CaseFiles.Add(photo);
                    }

                    if (dto.PrimaryPhotoId.HasValue)
                        _caseHelper.SetPrimaryImage(entity.CaseFiles, dto.PrimaryPhotoId.Value);

                    if (entity.Status != CaseStatus.Pending &&
                        entity.Status != CaseStatus.Deleted)
                    {
                        entity.PreviousStatus = entity.Status;
                        entity.Status = CaseStatus.Pending;
                    }

                    entity.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<LongTermMissingCase>().Update(entity);

                    return true;
                },
                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(uploadedPhotos.Select(x => x.ImagePath));

                    await _caseHelper.DeleteFacesAsync(
                        uploadedPhotos
                            .Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                            .Select(x => x.FaceId!),
                        entity.Id);

                    _logger.LogError(
                        ex,
                        "Failed to update LongTerm case {CaseId} by user {UserId}",
                        id,
                        userId);
                });

            _caseHelper.CleanupPhysicalFiles(filesToDelete);

            if (faceIdsToDelete.Count > 0)
                await _caseHelper.DeleteFacesAsync(faceIdsToDelete, entity.Id);

            _logger.LogInformation(
                "Updated case {CaseId} by user {UserId}",
                id,
                userId);

            return ApiResponse<string>.Ok(message: "LongTerm case updated successfully.");
        }
            
    }
}