using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.LongTermCase.Response;
using SafeTrace.Application.DTOs.LongTermCase.Request;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.Exceptions;


namespace SafeTrace.Application.Services.Cases
{
    public class LongTermCaseService : BaseCasesService<LongTermMissingCase, LongTermCaseListDto, LongTermCaseDetailDto, LongTermCaseFilterDto>, ILongTermCaseService
    {
        private readonly IFileStorageService _fileStorageService;

        private const string FolderName = "LongTermCase";
        private const string PoliceReportsFolder = "LongTermCase/PoliceReports";

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

        /// <summary>
        /// Deletes the police report image when a long-term case is permanently deleted.
        /// </summary>
        protected override void DeleteAdditionalFiles(LongTermMissingCase entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.PoliceReportImage))
            {
                _caseHelper.CleanupPhysicalFiles([entity.PoliceReportImage]);
            }
        }

        /// <summary>
        /// Creates a new long-term missing case with pending status.
        /// Before creating, the user must be verified, and the case is checked against existing
        /// active cases (via face + attribute matching):
        /// - a match with the SAME case type (LongTerm) blocks creation entirely (true duplicate).
        /// - a match with a DIFFERENT case type blocks creation and returns the matched case(s),
        ///   unless forceCreate is true.
        /// </summary>
        public async Task<ApiResponse<string>> CreateAsync(string userId, CreateLongTermCaseDto dto, bool forceCreate = false)
        {
            await _caseHelper.ValidateVerifiedUserAsync(userId);

            var subject = new CaseMatchSubjectInfoDto
            {
                Gender = dto.Gender,
                Age = dto.Age
            };

            var matches = await _caseHelper.FindMatchedCasesAsync(subject, dto.PrimaryImage);

            var checkResult = _caseHelper.CheckDuplicateCase(CaseType.LongTerm, matches);

            if (checkResult.HasSameTypeMatch)
            {
                throw new BadRequestException($"توجد حالة بنفس النوع بالفعل (كود الحالة: {checkResult?.SameTypeMatch?.CaseCode}).");
            }

            if (checkResult.HasCrossTypeMatches && !forceCreate)
            {
                throw new DuplicateCasesFoundException(checkResult.CrossTypeMatches);
            }

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
                entity.PoliceReportImage = await _fileStorageService.SaveFileAsync(dto.PoliceReportImage, PoliceReportsFolder);
            }

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    var uploadedPhotos = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        FolderName,
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

                    _logger.LogError(
                        ex,
                        "Failed to create longTerm case for user {UserId}",
                        userId);
                });

            _logger.LogInformation(
                "Created longTerm missing case. CaseId={CaseId}, CaseCode={CaseCode}, UserId={UserId}",
                entity.Id, entity.CaseCode, userId);

            return ApiResponse<string>.Ok("تم إنشاء حالة الفقد طويلة المدة بنجاح.");
        }

        /// <summary>
        /// Updates a longTerm missing case with photo and police report management.
        /// </summary>
        public async Task<ApiResponse<string>> UpdateAsync(long id, string userId, UpdateLongTermCaseDto dto)
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
                    _mapper.Map(dto, entity);

                    entity.Age = dto.Age;
                    entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
                    entity.UpdatedAt = DateTime.UtcNow;

                    if (dto.PoliceReportImage is not null)
                    {
                        var newReport = await _fileStorageService.SaveFileAsync(dto.PoliceReportImage, PoliceReportsFolder);

                        _fileStorageService.DeleteFile(entity.PoliceReportImage);

                        entity.PoliceReportImage = newReport;
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

                    if (dto.NewPhotos?.Count > 0)
                    {
                        var newPhotos = await _caseHelper.CreateCaseFilesAsync(
                            dto.NewPhotos.First(),
                            dto.NewPhotos.Skip(1),
                            null,
                            FolderName,
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

            return ApiResponse<string>.Ok(message: "تم تحديث حالة الفقد طويلة المدة بنجاح.");
        }

    }
}