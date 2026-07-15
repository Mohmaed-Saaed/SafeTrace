using Microsoft.AspNetCore.Identity;
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
      
        public async Task<ApiResponse<CreateCaseResultDto>> CreateUnknownCaseAsync(string userId, CreateUnknownDto dto, bool forceCreate = false)
        {
            await _caseHelper.ValidateVerifiedUserAsync(userId);

            var subject = new CaseMatchSubjectInfoDto
            {
                Gender = dto.Gender,
                Age = dto.Age
            };

            // نلتقط الـ match الجاهز من CheckDuplicateCaseAsync بدل تنفيذ Logic
            // بيعتمد على entity.Id، وهو لسه مش موجود في اللحظة دي
            MatchedCaseDto? pendingSameTypeMatch = null;

            var checkResult = await _caseHelper.CheckDuplicateCaseAsync(
                CaseType.Unknown,
                subject,
                dto.PrimaryImage,
                onSameTypeMatchAsync: duplicate =>
                {
                    pendingSameTypeMatch = duplicate;
                    return Task.CompletedTask;
                },
                forceCreate);

            // Unknown + LongTerm/Urgent -> نفس السلوك الحالي بالظبط: نرجع للمستخدم يقرر
            if (checkResult.RequiresConfirmation)
            {
                return ApiResponse<CreateCaseResultDto>.Ok(
                    new CreateCaseResultDto
                    {
                        IsCreated = false,
                        MatchedCases = checkResult.MatchedCases
                    });
            }

            var entity = _mapper.Map<UnknownCase>(dto);
            entity.UserId = userId;
            entity.Status = CaseStatus.Pending;
            entity.CaseType = CaseType.Unknown;
            entity.CreatedAt = DateTime.UtcNow;
            entity.AgeCategoryId =
                await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

            entity.CaseCode =
                await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.UNK);

            var uploadedFiles = new List<CaseFile>();

            await ExecuteInTransactionAsync(

                action: async () =>
                {
                    await _unitOfWork
                        .Repository<UnknownCase>()
                        .CreateAsync(entity);

                    await _unitOfWork.SaveAsync();

                    uploadedFiles = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        FolderName,
                        entity.Id);

                    entity.CaseFiles = uploadedFiles;

                    // entity.Id موجود دلوقتي، ونستخدم الـ match اللي اتلقط قبل كده
                    // (نفس الـ 80% + Gender + Age اللي CheckDuplicateCaseAsync استخدمهم)
                    // بدل عمل بحث Face Recognition جديد بمعايير مختلفة (95% بدون فلترة)
                    await _caseHelper.LinkCaseToDuplicateGroupAsync(
                        entity,
                        pendingSameTypeMatch);

                    _unitOfWork
                        .Repository<UnknownCase>()
                        .Update(entity);

                    return true;
                },

                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(
                        uploadedFiles.Select(x => x.ImagePath));

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

            return ApiResponse<CreateCaseResultDto>.Ok(
                new CreateCaseResultDto
                {
                    IsCreated = true,
                    CaseId = entity.Id
                });
        }

        public override async Task<ApiResponse<PaginationResponseDto<UnknownCaseListDto>>> GetAllAsync(
        UnknownCasesFilterDto filter)
        {
            var query = _unitOfWork
                .Repository<UnknownCase>()
                .Query(
                    tracked: false,
                    includes:
                    [
                        x => x.CaseFiles,
                x => x.DuplicateGroups
                    ])
                .Where(x => x.Status == CaseStatus.Active);
            query = query
    .GroupBy(x =>
        x.DuplicateGroups.Any()
            ? x.DuplicateGroups.First().DuplicateGroupId
            : -x.Id)
    .Select(g => g
        .OrderByDescending(x => x.CreatedAt)
        .First());

            var response = await GetPagedResultAsync<UnknownCaseListDto>(
                query,
                filter);

            return ApiResponse<PaginationResponseDto<UnknownCaseListDto>>
                .Ok(response, "تم استرجاع الحالات بنجاح.");
        }

     public override async Task<ApiResponse<UnknownCaseDetailDto>> GetByIdAsync(long id)
        { 
    var dto = await GetByIdInternalAsync<UnknownCaseDetailDto>(
        id,
        activeOnly: true,
        includes:
        [
            x => x.CaseFiles,
            x => x.User,
            x => x.AgeCategory
        ]);
    var groupId = await _unitOfWork
        .Repository<DuplicateGroupCase>()
        .Query(tracked: false)
        .Where(x => x.CaseId == id)
        .Select(x => (long?)x.DuplicateGroupId)
        .FirstOrDefaultAsync();

    if (groupId != null)
    {
        var relatedCases = await _unitOfWork
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
                x.CaseId != id)
            .OrderByDescending(x => x.Case.CreatedAt)
            .ToListAsync();

        dto.RelatedCases = relatedCases
            .Select(x => new RelatedUnknownCaseDto
            {
                Id = x.Case.Id,
                CaseCode = x.Case.CaseCode,
                CreatedAt = x.Case.CreatedAt,
                Similarity = (float)x.SimilarityScore,
                MainPhotoPath = x.Case.CaseFiles
                    .Where(f => f.IsPrimary)
                    .Select(f => f.ImagePath)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToList();
    }

    return ApiResponse<UnknownCaseDetailDto>.Ok(
        dto,
        "تم استرجاع بيانات الحالة بنجاح.");
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