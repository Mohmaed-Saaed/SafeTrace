using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.DTOs.UnKnownCase.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.Application.Services.Cases
{
    public class UnKnownCaseService : BaseCasesService<UnknownCase, UnknownCaseListDto, UnknownCaseDetailDto, UnknownCasesFilterDto>, IUnknownCaseService
    {
        private const string FolderName = "UnknownCase";
        private readonly IFileStorageService _fileStorageService;

        public UnKnownCaseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
             IFileStorageService fileStorageService,
            ILogger<UnKnownCaseService> logger)
            : base(unitOfWork, mapper, caseHelper, logger)
        {
            _fileStorageService = fileStorageService;
        }
      
        /// <summary>
        /// Customizes the base query used by GetAllAsync (inherited from BaseCasesService):
        /// includes DuplicateGroups and collapses each duplicate group down to its most recent case.
        /// Shared filtering, sorting, pagination, and mapping are still handled by GetPagedResultAsync
        /// in the base class via the inherited GetAllAsync.
        /// </summary>
        protected override IQueryable<UnknownCase> BuildGetAllQuery()
        {
            var latestCaseIds = _unitOfWork
                .Repository<DuplicateGroupCase>()
                .Query(tracked: false)
                .Where(x => x.Case.Status == CaseStatus.Active)
                .GroupBy(x => x.DuplicateGroupId)
                .Select(g => g
                    .OrderByDescending(x => x.Case.CreatedAt)
                    .Select(x => x.CaseId)
                    .First());

            return _unitOfWork
                .Repository<UnknownCase>()
                .Query(
                    tracked: false,
                    includes:
                    [
                        x => x.CaseFiles,
                        x => x.User,
                        x => x.DuplicateGroups
                    ])
                .Where(x =>
                    x.Status == CaseStatus.Active &&
                    (
                        !x.DuplicateGroups.Any() ||
                        latestCaseIds.Contains(x.Id)
                    ));
        }

        /// <summary>
        /// Populates RelatedCases on the mapped DTO after the base class's GetByIdAsync
        /// has retrieved and mapped the entity. Other case types are unaffected since this
        /// hook is a no-op in BaseCasesService by default.
        /// </summary>
        protected override async Task AfterGetByIdAsync(
            UnknownCaseDetailDto dto,
            long id,
            bool isAdmin)
        {
            var groupId = await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .Query(tracked: false)
                .Where(x => x.CaseId == id)
                .Select(x => (long?)x.DuplicateGroupId)
                .FirstOrDefaultAsync();

            if (groupId == null)
                return;

            var query = _unitOfWork
                .Repository<DuplicateGroupCase>()
                .Query(
                    tracked: false,
                    includes:
                    [
                        x => x.Case,
                        x => x.Case.CaseFiles
                    ])
                .Where(x =>
                    x.DuplicateGroupId == groupId &&
                    x.CaseId != id);

            // المستخدم العادي يشوف الحالات الـ Active فقط
            if (!isAdmin)
            {
                query = query.Where(x => x.Case.Status == CaseStatus.Active);
            }

            var relatedCases = await query
                .OrderByDescending(x => x.Case.CreatedAt)
                .ToListAsync();

            dto.RelatedCases = relatedCases
                .Select(x => new RelatedUnknownCaseDto
                {
                    Id = x.Case.Id,
                    CaseCode = x.Case.CaseCode,
                    CreatedAt = x.Case.CreatedAt,
                    Similarity = x.SimilarityScore.HasValue
                   ? (float)x.SimilarityScore.Value
                    : null,
                    MainPhotoPath = x.Case.CaseFiles
                        .Where(f => f.IsPrimary)
                        .Select(f => f.ImagePath)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToList();
        }

        public async Task<ApiResponse<CreateCaseResponseDto>> CreateUnknownCaseAsync(string userId, CreateUnknownDto dto, bool forceCreate = false)
        {
            await _caseHelper.ValidateVerifiedUserAsync(userId);

            await _caseHelper.ValidateUploadedImagesIdentityAsync(dto.PrimaryImage, dto.AdditionalImages);

            var duplicateCheck = await _caseHelper.CheckDuplicateCaseAsync(CaseType.Unknown, dto.PrimaryImage, userId);

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

            var sameTypeMatch = duplicateCheck.MatchedCases
                .FirstOrDefault(c => c.CaseType == CaseType.Unknown);

            var entity = _mapper.Map<UnknownCase>(dto);
            entity.UserId = userId;
            entity.Status = CaseStatus.Pending;
            entity.CaseType = CaseType.Unknown;
            entity.CreatedAt = DateTime.UtcNow;
            entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.UNK);

            var uploadedFiles = new List<CaseFile>();

            await ExecuteInTransactionAsync(

                action: async () =>
                {
                    await _unitOfWork
                        .Repository<UnknownCase>()
                        .CreateAsync(entity);

                    uploadedFiles = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        FolderName,
                        entity.Id);

                    entity.CaseFiles = uploadedFiles;

                    await LinkCaseToDuplicateGroupAsync(
                        entity,
                        sameTypeMatch);

                    return true;
                },

                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(uploadedFiles.Select(x => x.ImagePath));

                    await _caseHelper.DeleteFacesAsync(
                        uploadedFiles
                            .Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                            .Select(x => x.FaceId!),
                        entity.Id);

                    _logger.LogError(
                        ex,
                        "Failed to create unknown case for user {UserId}",
                        userId);
                });

            _logger.LogInformation(
                "Unknown case {CaseCode} created successfully.",
                entity.CaseCode);

            return ApiResponse<CreateCaseResponseDto>.Ok(
                new CreateCaseResponseDto
                {
                    IsCreated = true,
                    IsBlocked = false,
                    CaseId = entity.Id,
                    MatchedCases = duplicateCheck.MatchedCases
                });
        }

        public async Task<ApiResponse<string>> UpdateUnknownCaseAsync(
            long id,
            string userId,
            UpdateUnknownCaseDto dto)
        {
            var entity = await _caseHelper.GetValidCaseAsync<UnknownCase>(
                id,
                includes: [x => x.CaseFiles]);

            if (!string.Equals(entity.UserId, userId, StringComparison.Ordinal))
            {
                throw new ForbiddenException("You cannot update another user's case.");
            }

            _caseHelper.ValidateCaseIsEditable(entity);

            if (dto.AdditionalImages is { Count: > 0 })
            {
                throw new BadRequestException(
                    "AdditionalImages is create-only. Use NewPhotos when updating a case.");
            }

            if (dto.IsExistingVideoDeleted && dto.Video is not null)
            {
                throw new BadRequestException(
                    "Cannot upload a replacement video while deleting the existing video.");
            }

            _caseHelper.ValidateUpdateMediaState(
                entity.CaseFiles,
                dto.PrimaryImage,
                dto.NewPhotos,
                dto.DeletedPhotoIds);

            var existingFaceIds = entity.CaseFiles
                .Where(f =>
                    f.Type == FileType.Image &&
                    !string.IsNullOrWhiteSpace(f.FaceId))
                .Select(f => f.FaceId!)
                .Distinct()
                .ToList();

            // Same-person verification only; update never performs duplicate detection.
            await _caseHelper.ValidateUploadedImagesIdentityAsync(
                dto.PrimaryImage,
                dto.NewPhotos,
                existingFaceIds);

            var uploadedPhotos = new List<CaseFile>();
            var filesToDelete = new List<string>();
            var faceIdsToDelete = new List<string>();
            var newAdditionalPhotos = new List<CaseFile>();
            CaseFile? newPrimary = null;
            CaseFile? newVideo = null;
            var ageCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(dto.Age);

            try
            {
                if (dto.PrimaryImage is not null)
                {
                    var primaryFiles = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        null,
                        null,
                        FolderName,
                        entity.Id,
                        requireFaceIndexing: true);
                    newPrimary = primaryFiles.Single();
                    uploadedPhotos.Add(newPrimary);
                }

                if (dto.NewPhotos is { Count: > 0 })
                {
                    newAdditionalPhotos = await _caseHelper.CreateAdditionalCaseFilesAsync(
                        dto.NewPhotos,
                        FolderName,
                        entity.Id,
                        requireFaceIndexing: true);
                    uploadedPhotos.AddRange(newAdditionalPhotos);
                }

                if (!dto.IsExistingVideoDeleted && dto.Video is not null)
                {
                    var videoPath = await _fileStorageService.SaveFileAsync(
                        dto.Video,
                        FolderName);
                    newVideo = new CaseFile
                    {
                        ImagePath = videoPath,
                        CaseId = entity.Id,
                        IsPrimary = false,
                        Type = FileType.Video,
                        CreatedAt = DateTime.UtcNow
                    };
                    uploadedPhotos.Add(newVideo);
                }

                _caseHelper.ValidateFinalUpdateIdentityAnchor(
                    entity.CaseFiles,
                    uploadedPhotos,
                    dto.DeletedPhotoIds,
                    newPrimary is not null);
            }
            catch
            {
                _caseHelper.CleanupPhysicalFiles(
                    uploadedPhotos.Select(x => x.ImagePath));
                await _caseHelper.DeleteFacesAsync(
                    uploadedPhotos
                        .Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                        .Select(x => x.FaceId!),
                    entity.Id);
                throw;
            }

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    if (entity.Status != CaseStatus.Deleted)
                    {
                        entity.PreviousStatus = entity.Status;
                        entity.Status = CaseStatus.Pending;
                    }

                    entity.Gender = dto.Gender;
                    entity.FName = dto.FName ?? entity.FName;
                    entity.SName = dto.SName ?? entity.SName;
                    entity.TName = dto.TName ?? entity.TName;
                    entity.LName = dto.LName ?? entity.LName;
                    entity.Age = dto.Age;
                    entity.AgeCategoryId = ageCategoryId;
                    entity.Government = dto.Government;
                    entity.City = dto.City;
                    entity.Street = dto.Street ?? entity.Street;
                    entity.CommunicationPhone =
                        dto.CommunicationPhone ?? entity.CommunicationPhone;
                    entity.Description = dto.Description ?? entity.Description;

                    // =====================================================
                    // Delete old photos
                    // =====================================================
                    if (dto.DeletedPhotoIds?.Count > 0)
                    {
                        var toRemove = entity.CaseFiles
                            .Where(p =>
                                dto.DeletedPhotoIds.Contains(p.Id) &&
                                p.Type == FileType.Image)
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

                    // =====================================================
                    // Add new photos
                    // =====================================================
                    foreach (var photo in newAdditionalPhotos)
                    {
                        entity.CaseFiles.Add(photo);
                    }

                    // =====================================================
                    // DELETE / REPLACE VIDEO
                    // =====================================================
                    var oldVideo = entity.CaseFiles
                        .FirstOrDefault(f => f.Type == FileType.Video);

                    if (dto.IsExistingVideoDeleted)
                    {
                        if (oldVideo is not null)
                        {
                            filesToDelete.Add(oldVideo.ImagePath);
                            entity.CaseFiles.Remove(oldVideo);
                        }
                    }
                    else if (newVideo is not null)
                    {
                        if (oldVideo is not null)
                        {
                            filesToDelete.Add(oldVideo.ImagePath);
                            entity.CaseFiles.Remove(oldVideo);
                        }

                        entity.CaseFiles.Add(newVideo);
                    }



                    // =====================================================
                    // Replace Primary Image
                    // =====================================================
                    if (newPrimary is not null)
                    {
                        var oldPrimaries = entity.CaseFiles
                            .Where(p => p.IsPrimary && p.Type == FileType.Image)
                            .ToList();

                        foreach (var oldPrimary in oldPrimaries)
                        {
                            filesToDelete.Add(oldPrimary.ImagePath);

                            if (!string.IsNullOrWhiteSpace(oldPrimary.FaceId))
                            {
                                faceIdsToDelete.Add(oldPrimary.FaceId);
                            }

                            entity.CaseFiles.Remove(oldPrimary);
                        }

                        newPrimary.IsPrimary = true;

                        entity.CaseFiles.Add(newPrimary);
                    }

                    // =====================================================
                    // Update timestamp
                    // =====================================================
                    entity.UpdatedAt = DateTime.UtcNow;

                    // =====================================================
                    // Update entity
                    // =====================================================
                    _unitOfWork
                        .Repository<UnknownCase>()
                        .Update(entity);

                    return true;
                },

                onFailureAsync: async ex =>
                {
                    // =====================================================
                    // Delete newly uploaded physical files
                    // =====================================================
                    _caseHelper.CleanupPhysicalFiles(
                        uploadedPhotos.Select(x => x.ImagePath));

                    // =====================================================
                    // Delete newly created faces
                    // =====================================================
                    await _caseHelper.DeleteFacesAsync(
                        uploadedPhotos
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x.FaceId))
                            .Select(x => x.FaceId!),
                        entity.Id);

                    _logger.LogError(
                        ex,
                        "Failed to update unknown case {CaseId} for user {UserId}",
                        id,
                        userId);
                });

            // =========================================================
            // Delete old physical files
            // =========================================================
            _caseHelper.CleanupPhysicalFiles(filesToDelete);

            // =========================================================
            // Delete removed faces
            // =========================================================
            if (faceIdsToDelete.Count > 0)
            {
                await _caseHelper.DeleteFacesAsync(
                    faceIdsToDelete,
                    entity.Id);
            }

            _logger.LogInformation(
                "Unknown case {CaseId} updated successfully.",
                entity.Id);

            return ApiResponse<string>.Ok(
                message: "تم تحديث حالة مجهول الهوية بنجاح");
        }
        private async Task LinkCaseToDuplicateGroupAsync(UnknownCase newCase, MatchedCaseDto? sameTypeMatch)
        {
            if (sameTypeMatch == null)
            {
                await CreateDuplicateGroupAsync(newCase);
                return;
            }

            var matchedCase = await GetMatchedCaseAsync(sameTypeMatch.Id, newCase.Id);

            if (matchedCase == null)
            {
                await CreateDuplicateGroupAsync(newCase);
                return;
            }

            var groupLink = await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .Query(tracked: true)
                .FirstOrDefaultAsync(x => x.CaseId == matchedCase.Id);

            if (groupLink == null)
            {
                await CreateDuplicateGroupWithCasesAsync(
                    matchedCase,
                    newCase,
                    (decimal)sameTypeMatch.Similarity);

                return;
            }

            await AddCaseToGroupAsync(
                groupLink.DuplicateGroupId,
                newCase,
                (decimal)sameTypeMatch.Similarity);
        }

        private async Task<UnknownCase?> GetMatchedCaseAsync(long caseId, long currentCaseId)
        {
            return await _unitOfWork
                .Repository<UnknownCase>()
                .Query(tracked: true)
                .Where(x =>
                    x.Id == caseId &&
                    x.Id != currentCaseId &&
                    x.Status != CaseStatus.Deleted)
                .FirstOrDefaultAsync();
        }

        private async Task CreateDuplicateGroupAsync(UnknownCase newCase)
        {
            var group = new DuplicateGroup
            {
                GroupStatus = DuplicateGroupStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork
                .Repository<DuplicateGroup>()
                .CreateAsync(group);

            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroup = group,
                    Case = newCase,
                    SimilarityScore = null,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });
        }

        private async Task CreateDuplicateGroupWithCasesAsync(UnknownCase oldCase, UnknownCase newCase, decimal similarity)
        {
            var group = new DuplicateGroup
            {
                GroupStatus = DuplicateGroupStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork
                .Repository<DuplicateGroup>()
                .CreateAsync(group);

            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroup = group,
                    Case = oldCase,
                    SimilarityScore = null,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });

            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroup = group,
                    Case = newCase,
                    SimilarityScore = similarity,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });
        }

        private async Task AddCaseToGroupAsync(long groupId, UnknownCase newCase, decimal similarity)
        {
            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroupId = groupId,
                    Case = newCase,
                    SimilarityScore = similarity,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });
        }

    }
}
