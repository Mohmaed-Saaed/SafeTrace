using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Cases.Request;
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
        
        public UnKnownCaseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
            ILogger<UnKnownCaseService> logger)
            : base(unitOfWork, mapper, caseHelper, logger)
        {
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

            var duplicateCheck = await _caseHelper.CheckDuplicateCaseAsync(CaseType.Unknown, dto.PrimaryImage);

            if (duplicateCheck.MatchedCases.Count > 0)
            {
                var isBlocked = duplicateCheck.MatchedCases.Any(c => c.CaseType == CaseType.LongTerm || c.CaseType == CaseType.Urgent);

                if (isBlocked)
                {
                    return ApiResponse<CreateCaseResponseDto>.Ok(
                        new CreateCaseResponseDto
                        {
                            IsCreated = false,
                            IsBlocked = true,
                            MatchedCases = duplicateCheck.MatchedCases
                        });
                }

                if (!forceCreate)
                {
                    return ApiResponse<CreateCaseResponseDto>.Ok(
                        new CreateCaseResponseDto
                        {
                            IsCreated = false,
                            IsBlocked = false,
                            MatchedCases = duplicateCheck.MatchedCases
                        });
                }
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
            // NOTE: PrimaryImage always nets +1 photo (replace old primary with new one),
            // so it doesn't change the count unless there was no primary before —
            // it's excluded from this guard on purpose since it's a 1:1 replacement.

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

                    // ── FIX: PrimaryImage (crop-and-replace primary photo from the edit
                    // form) was never handled here — the frontend sends it, but nothing
                    // ever consumed it, so a "replaced" primary photo silently vanished.
                    // Takes precedence over PrimaryPhotoId, matching frontend behavior.
                    if (dto.PrimaryImage is not null)
                    {
                        var oldPrimary = entity.CaseFiles.FirstOrDefault(p => p.IsPrimary);

                        var newPrimaryPhotos = await _caseHelper.CreateCaseFilesAsync(
                            dto.PrimaryImage,
                            null,
                            null,
                            FolderName,
                            entity.Id);

                        uploadedPhotos.AddRange(newPrimaryPhotos);
                        var newPrimary = newPrimaryPhotos.First();

                        if (oldPrimary is not null)
                        {
                            filesToDelete.Add(oldPrimary.ImagePath);

                            if (!string.IsNullOrWhiteSpace(oldPrimary.FaceId))
                                faceIdsToDelete.Add(oldPrimary.FaceId);

                            entity.CaseFiles.Remove(oldPrimary);
                        }

                        foreach (var f in entity.CaseFiles)
                            f.IsPrimary = false;

                        newPrimary.IsPrimary = true;
                        entity.CaseFiles.Add(newPrimary);
                    }
                    else if (dto.PrimaryPhotoId.HasValue)
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