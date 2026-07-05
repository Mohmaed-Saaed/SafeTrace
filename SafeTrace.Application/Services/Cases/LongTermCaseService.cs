using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.LongTermCases.Request;
using SafeTrace.Application.DTOs.LongTermCases.Response;

namespace SafeTrace.Application.Services.Cases
{
    public class LongTermCaseService
        : BaseCasesService<LongTermMissingCase, LongTermCaseListDto, LongTermCaseDetailDto, LongTermCaseFilterDto>, ILongTermCaseService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFaceRecognitionService _faceRecognition;

        public LongTermCaseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorage,
            ICaseHelperService caseHelper,
            IFaceRecognitionService faceRecognition,
            ILogger<LongTermCaseService> logger)
            : base(unitOfWork, mapper, caseHelper, logger)
        {
            _fileStorageService = fileStorage;
            _faceRecognition = faceRecognition;
        }

        protected override async Task DeleteAdditionalFilesAsync(LongTermMissingCase entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.PoliceReportImage))
                await _caseHelper.CleanupPhysicalFilesAsync(Enumerable.Empty<string>());
        }

        // CREATE
        public async Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId)
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
                entity.PoliceReportImage = await _fileStorageService.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");

            if (dto.Photos is not null && dto.Photos.Any())
            {
                var photoList = dto.Photos.ToList();

                for (int i = 0; i < photoList.Count; i++)
                {
                    var photo = photoList[i];
                    var path = await _fileStorageService.SaveFileAsync(photo, "long-term");

                    bool isPrimary = dto.PrimaryPhotoIndex == i;
                    string? faceId = null;
                    try
                    {
                        faceId = await _faceRecognition.IndexFaceAsync(photo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "تعذّر تسجيل الوجه في AWS للصورة {Index} أثناء إنشاء الحالة للمستخدم {UserId}.", i, userId);
                    }

                    entity.CaseFiles.Add(new CaseFile
                    {
                        ImagePath = path,
                        FaceId = faceId,
                        IsPrimary = isPrimary
                    });
                }

                _caseHelper.EnsureSinglePrimaryPhoto(entity.CaseFiles);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LongTermMissingCase>().CreateAsync(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "تم إنشاء حالة مفقود طويل الأمد. CaseId={CaseId}, CaseCode={CaseCode}, UserId={UserId}",
                    entity.Id, entity.CaseCode, userId);

                return entity.Id;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                foreach (var photo in entity.CaseFiles)
                    _fileStorageService.DeleteFile(photo.ImagePath);

                if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                    _fileStorageService.DeleteFile(entity.PoliceReportImage);

                _logger.LogError(ex, "فشل إنشاء الحالة للمستخدم {UserId}", userId);
                throw;
            }
        }

        // UPDATE
        public async Task UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin)
        {
            var entity = await _caseHelper.GetValidCaseAsync<LongTermMissingCase>(
                id,
                userId,
                isAdmin,
                checkOwnership: true,
                includes: [c => c.CaseFiles]
            );

            if (dto.Age.HasValue)
            {
                entity.Age = dto.Age.Value;
                entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
            }

            if (dto.Gender.HasValue) entity.Gender = dto.Gender.Value;
            if (dto.FName is not null) entity.FName = dto.FName;
            if (dto.SName is not null) entity.SName = dto.SName;
            if (dto.TName is not null) entity.TName = dto.TName;
            if (dto.LName is not null) entity.LName = dto.LName;
            if (dto.Relation.HasValue) entity.Relation = dto.Relation.Value;
            if (dto.Description is not null) entity.Description = dto.Description;
            if (dto.Government is not null) entity.Government = dto.Government;
            if (dto.City is not null) entity.City = dto.City;
            if (dto.Street is not null) entity.Street = dto.Street;

            if (dto.PoliceReportImage is not null)
            {
                if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                    _fileStorageService.DeleteFile(entity.PoliceReportImage);

                entity.PoliceReportImage = await _fileStorageService.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");
            }

            if (dto.RemovedPhotoIds is { Count: > 0 })
            {
                var toRemove = entity.CaseFiles.Where(p => dto.RemovedPhotoIds.Contains(p.Id)).ToList();
                foreach (var photo in toRemove)
                {
                    _fileStorageService.DeleteFile(photo.ImagePath);

                    if (!string.IsNullOrEmpty(photo.FaceId))
                    {
                        try
                        {
                            await _faceRecognition.DeleteFaceAsync(photo.FaceId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "تعذّر حذف الوجه {FaceId} من AWS عند تعديل الحالة {CaseId}.", photo.FaceId, id);
                        }
                    }

                    entity.CaseFiles.Remove(photo);
                    _unitOfWork.Repository<CaseFile>().Remove(photo);
                }
            }

            if (dto.NewPhotos is not null && dto.NewPhotos.Any())
            {
                var newPhotoList = dto.NewPhotos.ToList();
                for (int i = 0; i < newPhotoList.Count; i++)
                {
                    var photo = newPhotoList[i];
                    var path = await _fileStorageService.SaveFileAsync(photo, "long-term");

                    string? faceId = null;
                    try
                    {
                        faceId = await _faceRecognition.IndexFaceAsync(photo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "تعذّر تسجيل الوجه في AWS للصورة الجديدة {Index} عند تعديل الحالة {CaseId}.", i, id);
                    }

                    entity.CaseFiles.Add(new CaseFile
                    {
                        ImagePath = path,
                        FaceId = faceId,
                        IsPrimary = false
                    });
                }
            }

            _caseHelper.EnsureSinglePrimaryPhoto(entity.CaseFiles, dto.PrimaryPhotoId);

            if (!isAdmin && entity.Status != CaseStatus.Pending)
            {
                entity.PreviousStatus = entity.Status;
                entity.Status = CaseStatus.Pending;
            }

            _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("تم تعديل الحالة {CaseId} بواسطة المستخدم {UserId} (IsAdmin={IsAdmin})", id, userId, isAdmin);
        }
    }
}