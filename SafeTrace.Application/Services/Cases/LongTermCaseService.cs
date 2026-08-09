using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.LongTermCase.Response;
using SafeTrace.Application.DTOs.LongTermCase.Request;
using SafeTrace.Application.DTOs.Cases.Response;

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
        public async Task<ApiResponse<CreateCaseResponseDto>> CreateAsync(string userId, CreateLongTermCaseDto dto, bool forceCreate = false)
        {
            await _caseHelper.ValidateVerifiedUserAsync(userId);

            await _caseHelper.ValidateUploadedImagesIdentityAsync(dto.PrimaryImage, dto.AdditionalImages);

            var duplicateCheck = await _caseHelper.CheckDuplicateCaseAsync(CaseType.LongTerm, dto.PrimaryImage, userId);

            if (duplicateCheck.IsBlocked)
            {
                return ApiResponse<CreateCaseResponseDto>.Ok(
                    new CreateCaseResponseDto
                    {
                        IsCreated = false,
                        IsBlocked = true,
                        DuplicateDecision = duplicateCheck.DuplicateDecision,
                        ExistingCaseId = duplicateCheck.ExistingCaseId,
                        ExistingCaseType = duplicateCheck.ExistingCaseType,
                        ExistingStatus = duplicateCheck.ExistingStatus,
                        MatchedCases = duplicateCheck.MatchedCases
                    });
            }
            
            if (duplicateCheck.DuplicateDecision != DuplicateDecision.None && !forceCreate)
            {
                return ApiResponse<CreateCaseResponseDto>.Ok(
                    new CreateCaseResponseDto
                    {
                        IsCreated = false,
                        IsBlocked = false,
                        DuplicateDecision = duplicateCheck.DuplicateDecision,
                        ExistingCaseId = duplicateCheck.ExistingCaseId,
                        ExistingCaseType = duplicateCheck.ExistingCaseType,
                        ExistingStatus = duplicateCheck.ExistingStatus,
                        MatchedCases = duplicateCheck.MatchedCases
                    });
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

                    await _unitOfWork.Repository<LongTermMissingCase>()
                        .CreateAsync(entity);

                    return true;
                },
                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(
                        entity.CaseFiles.Select(x => x.ImagePath));

                    _fileStorageService.DeleteFile(entity.PoliceReportImage);

                    await _caseHelper.DeleteFacesAsync(
                        entity.CaseFiles.Select(x => x.FaceId),
                        entity.Id);

                    _logger.LogError(
                        ex,
                        "Failed to create LongTerm case for user {UserId}",
                        userId);
                });

            _logger.LogInformation(
                "Created LongTerm case. CaseId={CaseId}, CaseCode={CaseCode}, UserId={UserId}",
                entity.Id,
                entity.CaseCode,
                userId);

            return ApiResponse<CreateCaseResponseDto>.Ok(
                new CreateCaseResponseDto
                {
                    IsCreated = true,
                    IsBlocked = false,
                    CaseId = entity.Id,
                    MatchedCases = []
                });
        }

        public async Task<ApiResponse<string>> UpdateAsync(
    long id,
    UpdateLongTermCaseDto dto)
        {
            var entity = await _caseHelper.GetValidCaseAsync<LongTermMissingCase>(
                id,
                includes: [c => c.CaseFiles]);

            _caseHelper.ValidateCaseIsEditable(entity);

            // Existing FaceIds are needed only for image validation.
            var existingFaceIds = entity.CaseFiles?
                .Where(f => !string.IsNullOrWhiteSpace(f.FaceId))
                .Select(f => f.FaceId!)
                .ToList();

            // Validate images only.
            // IMPORTANT:
            // Video must NOT be passed here because this validation
            // is related to face/image recognition.
            await _caseHelper.ValidateUploadedImagesIdentityAsync(
                dto.PrimaryImage,
                dto.NewPhotos,
                existingFaceIds);

            var uploadedPhotos = new List<CaseFile>();
            var filesToDelete = new List<string>();
            var faceIdsToDelete = new List<string>();

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    // ---------------------------------------------
                    // Basic information
                    // ---------------------------------------------
                    _mapper.Map(dto, entity);

                    entity.Age = dto.Age;

                    entity.AgeCategoryId =
                        await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

                    entity.UpdatedAt = DateTime.UtcNow;


                    // ---------------------------------------------
                    // Police Report
                    // ---------------------------------------------
                    if (dto.PoliceReportImage is not null)
                    {
                        var newReport =
                            await _fileStorageService.SaveFileAsync(
                                dto.PoliceReportImage,
                                PoliceReportsFolder);

                        _fileStorageService.DeleteFile(
                            entity.PoliceReportImage);

                        entity.PoliceReportImage = newReport;
                    }


                    // ---------------------------------------------
                    // Delete old photos
                    // ---------------------------------------------
                    if (dto.DeletedPhotoIds is { Count: > 0 })
                    {
                        var toRemove = entity.CaseFiles
                            .Where(p => dto.DeletedPhotoIds.Contains(p.Id))
                            .ToList();

                        foreach (var photo in toRemove)
                        {
                            filesToDelete.Add(photo.ImagePath);

                            if (!string.IsNullOrWhiteSpace(photo.FaceId))
                            {
                                faceIdsToDelete.Add(photo.FaceId);
                            }

                            entity.CaseFiles.Remove(photo);
                        }
                    }


                    // ---------------------------------------------
                    // Add new photos
                    // ---------------------------------------------
                    if (dto.NewPhotos?.Count > 0)
                    {
                        var newPhotos =
                            await _caseHelper.CreateCaseFilesAsync(
                                dto.NewPhotos.First(),
                                dto.NewPhotos.Skip(1),
                                null,
                                FolderName,
                                entity.Id);

                        uploadedPhotos.AddRange(newPhotos);

                        foreach (var photo in newPhotos)
                        {
                            entity.CaseFiles.Add(photo);
                        }
                    }

                    if (dto.Video is not null)
                    {
                        var videoPath =
                            await _fileStorageService.SaveFileAsync(
                                dto.Video,
                                FolderName);

                        var videoFile = new CaseFile
                        {
                            ImagePath = videoPath,
                            CaseId = entity.Id,
                            IsPrimary = false,
                            Type = FileType.Video
                        };

                        entity.CaseFiles.Add(videoFile);

                        uploadedPhotos.Add(videoFile);
                    }


                    // ---------------------------------------------
                    // Replace Primary Image
                    // ---------------------------------------------
                    if (dto.PrimaryImage is not null)
                    {
                        // Find current primary photo
                        var oldPrimary =
                            entity.CaseFiles.FirstOrDefault(
                                p => p.IsPrimary &&
                                     p.Type != FileType.Video);

                        var newPrimaryPhotos =
                            await _caseHelper.CreateCaseFilesAsync(
                                dto.PrimaryImage,
                                null,
                                null,
                                FolderName,
                                entity.Id);

                        uploadedPhotos.AddRange(newPrimaryPhotos);

                        var newPrimary = newPrimaryPhotos.First();

                        // Remove old primary
                        if (oldPrimary is not null)
                        {
                            filesToDelete.Add(oldPrimary.ImagePath);

                            if (!string.IsNullOrWhiteSpace(oldPrimary.FaceId))
                            {
                                faceIdsToDelete.Add(oldPrimary.FaceId);
                            }

                            entity.CaseFiles.Remove(oldPrimary);
                        }

                        // Make sure only images can be primary.
                        foreach (var file in entity.CaseFiles)
                        {
                            if (file.Type != FileType.Video)
                            {
                                file.IsPrimary = false;
                            }
                        }

                        newPrimary.IsPrimary = true;

                        entity.CaseFiles.Add(newPrimary);
                    }
                    else if (dto.PrimaryPhotoId.HasValue)
                    {
                        _caseHelper.SetPrimaryImage(
                            entity.CaseFiles,
                            dto.PrimaryPhotoId.Value);
                    }


                    // ---------------------------------------------
                    // Update status
                    // ---------------------------------------------
                    if (entity.Status != CaseStatus.Pending &&
                        entity.Status != CaseStatus.Deleted)
                    {
                        entity.PreviousStatus = entity.Status;
                        entity.Status = CaseStatus.Pending;
                    }

                    entity.UpdatedAt = DateTime.UtcNow;


                    // ---------------------------------------------
                    // Update entity
                    // ---------------------------------------------
                    _unitOfWork
                        .Repository<LongTermMissingCase>()
                        .Update(entity);

                    return true;
                },

                onFailureAsync: async ex =>
                {
                    // Delete physical files that were uploaded
                    // during the failed transaction.
                    _caseHelper.CleanupPhysicalFiles(
                        uploadedPhotos.Select(x => x.ImagePath));


                    // Delete newly created face IDs.
                    await _caseHelper.DeleteFacesAsync(
                        uploadedPhotos
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x.FaceId))
                            .Select(x => x.FaceId!),
                        entity.Id);


                    _logger.LogError(
                        ex,
                        "Failed to update LongTerm case {CaseId} by user {UserId}",
                        id,
                        entity.UserId);
                });


            // ---------------------------------------------
            // Cleanup deleted physical files
            // ---------------------------------------------
            _caseHelper.CleanupPhysicalFiles(filesToDelete);


            // ---------------------------------------------
            // Delete removed faces
            // ---------------------------------------------
            if (faceIdsToDelete.Count > 0)
            {
                await _caseHelper.DeleteFacesAsync(
                    faceIdsToDelete,
                    entity.Id);
            }


            _logger.LogInformation(
                "Updated case {CaseId} by user {UserId}",
                id,
                entity.UserId);


            return ApiResponse<string>.Ok(
                message: "تم تحديث حالة الفقد طويلة المدة بنجاح.");
        }
    }
}