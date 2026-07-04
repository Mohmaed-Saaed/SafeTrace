    using AutoMapper;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using SafeTrace.Application.Helpers;
    using SafeTrace.Application.Common.Models;
    using SafeTrace.Application.DTOs.LongTermCases;
    using SafeTrace.Application.Exceptions;
    using SafeTrace.Application.Extensions;
    using SafeTrace.Application.Interfaces.IServices;
    using SafeTrace.Domain.Common;
    using SafeTrace.Domain.Entities;
    using SafeTrace.Domain.Enums;
    using SafeTrace.Domain.Interfaces.IUnitOfWork;

    namespace SafeTrace.Application.Services
    {
        public class LongTermCaseService : ILongTermCaseService
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IMapper _mapper;
            private readonly IFileStorageService _fileStorage;
            private readonly IFaceRecognitionService _faceRecognition;
            private readonly ILogger<LongTermCaseService> _logger;
            private readonly FaceMatchingHelper _faceMatchingHelper;

        public LongTermCaseService(
                IUnitOfWork unitOfWork,
                IMapper mapper,
                IFileStorageService fileStorage,
                IFaceRecognitionService faceRecognition,
                ILogger<LongTermCaseService> logger,
                FaceMatchingHelper faceMatchingHelper)
            {
                _unitOfWork = unitOfWork;
                _mapper = mapper;
                _fileStorage = fileStorage;
                _faceRecognition = faceRecognition;
                _logger = logger;
                _faceMatchingHelper = faceMatchingHelper;
            }

            // ─────────────────────────────────────────────────────────────
            // READ
            // ─────────────────────────────────────────────────────────────

            public async Task<PagedResultDto<LongTermCaseCardDto>> GetAllAsync(LongTermCaseFilterDto filter)
            {
                var name = filter.Name?.Trim();
                var filterByAge = filter.AgeCategory.HasValue;
                var ageRange = filterByAge ? AgeCategoryHelper.GetRange(filter.AgeCategory!.Value) : (Min: 0, Max: 0);

                var query = _unitOfWork.Repository<LongTermMissingCase>()
                    .Query(tracked: false, includes: c => c.Photos)
                    .Where(c => c.Status == CaseStatus.Active)
                    .WhereIf(!string.IsNullOrEmpty(name), c =>
                        (c.FName ?? "").Contains(name!) ||
                        (c.SName ?? "").Contains(name!) ||
                        (c.TName ?? "").Contains(name!) ||
                        (c.LName ?? "").Contains(name!))
                    .WhereIf(filter.Gender.HasValue, c => c.Gender == filter.Gender!.Value)
                    .WhereIf(filterByAge, c => c.Age >= ageRange.Min && c.Age <= ageRange.Max);

                var totalCount = await query.CountAsync();

                var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
                var pageSize = filter.PageSize < 1 ? 12 : filter.PageSize;

                var items = filter.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt);

                var paged = await items
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResultDto<LongTermCaseCardDto>
                {
                    Items = _mapper.Map<IEnumerable<LongTermCaseCardDto>>(paged),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            public async Task<LongTermCaseDetailsDto> GetByIdAsync(long id, bool includeDeleted = false)
            {
                var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                    .GetOneAsync(
                        c => c.Id == id && (includeDeleted || c.Status != CaseStatus.Deleted),
                        tracked: false,
                        c => c.Photos,
                        c => c.FoundPersonInfo!,
                        c => c.User);

                if (entity is null)
                    throw new NotFoundException($"لم يتم العثور على الحالة رقم {id}.");

                return _mapper.Map<LongTermCaseDetailsDto>(entity);
            }

            public async Task<IEnumerable<LongTermCaseCardDto>> GetMyCasesAsync(string userId)
            {
                var items = await _unitOfWork.Repository<LongTermMissingCase>()
                    .Query(tracked: false, orderBy: c => c.CreatedAt, orderByDirection: OrderBy.Descending, includes: c => c.Photos)
                    .Where(c => c.UserId == userId && c.Status != CaseStatus.Deleted)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(items);
            }

            public async Task<IEnumerable<LongTermCaseCardDto>> GetFoundedCasesAsync()
            {
                var items = await _unitOfWork.Repository<LongTermMissingCase>()
                    .Query(tracked: false, orderBy: c => c.CreatedAt, orderByDirection: OrderBy.Descending, includes: c => c.Photos)
                    .Where(c => c.Status == CaseStatus.Found)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(items);
            }

            /// <summary>
            /// Admin-only: يرجع الحالات بناءً على الحالة المطلوبة (Pending, Deleted, Active, Found, Rejected, أو الكل).
            /// </summary>
            public async Task<IEnumerable<LongTermCaseCardDto>> GetAdminCasesAsync(CaseStatus? status)
            {
                var baseQuery = _unitOfWork.Repository<LongTermMissingCase>()
                    .Query(tracked: false, includes: c => c.Photos);

                IQueryable<LongTermMissingCase> filteredQuery;

                if (status.HasValue)
                {
                    filteredQuery = status.Value == CaseStatus.Deleted
                        ? baseQuery
                            .Where(c => c.Status == CaseStatus.Deleted)
                            .OrderByDescending(c => c.DeletedAt)
                        : baseQuery
                            .Where(c => c.Status == status.Value)
                            .OrderBy(c => c.CreatedAt);
                }
                else
                {
                    // بدون فلتر: يرجع كل الحالات مرتبة بالأحدث
                    filteredQuery = baseQuery.OrderByDescending(c => c.CreatedAt);
                }

                var items = await filteredQuery.ToListAsync();
                return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(items);
            }

            // ─────────────────────────────────────────────────────────────
            // CREATE
            // ─────────────────────────────────────────────────────────────

            public async Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId)
            {
            // 1. أول خطوة: البحث عن حالات مطابقة لصور Unknown فقط إذا لم يطلب المستخدم التخطي (ForceCreate = false)
            if (dto.Photos is not null && dto.Photos.Any() && !dto.ForceCreate)
            {
                // استدعاء الهيلبر الخارجي المخطط له
                await _faceMatchingHelper.CheckForUnknownCaseMatchAsync(dto.Photos);
            }

            // 2. فحص التكرار بناءً على البيانات النصية والصور (الميزة الثانية لمنع رفع نفس الحالة مرتين في الـ LongTerm)
            var normalizedFName = dto.FName?.Trim().ToUpperInvariant();
            var normalizedLName = dto.LName?.Trim().ToUpperInvariant();

            var candidateDuplicates = await _unitOfWork.Repository<LongTermMissingCase>()
                .Query(tracked: false, includes: c => c.Photos)
                .Where(c =>
                    c.Status != CaseStatus.Deleted &&
                    c.Status != CaseStatus.Rejected &&
                    c.FName != null && c.FName.ToUpper() == normalizedFName &&
                    c.LName != null && c.LName.ToUpper() == normalizedLName &&
                    c.Age == dto.Age &&
                    c.Gender == dto.Gender)
                .ToListAsync();

            if (candidateDuplicates.Any())
            {
                // استخدام الهيلبر الخارجي للتأكد بالصور
                var duplicateMatch = await _faceMatchingHelper.FindDuplicateByImageAsync(candidateDuplicates, dto.Photos);

                if (duplicateMatch is not null)
                {
                    _logger.LogWarning(
                        "محاولة إنشاء حالة مكررة للشخص ({FName} {LName}) من المستخدم {UserId}. الحالة الموجودة: {CaseCode}",
                        dto.FName, dto.LName, userId, duplicateMatch.CaseCode);

                    throw new ConflictException(
                        $"يوجد بالفعل بلاغ مفتوح لهذا الشخص برقم الحالة {duplicateMatch.CaseCode}. " +
                        $"يمكنك التواصل مع صاحب البلاغ عبر صفحة الحالة.");
                }
            }

            var entity = _mapper.Map<LongTermMissingCase>(dto);

                entity.UserId = userId;
                entity.CaseType = CaseType.LongTerm;
                entity.Status = CaseStatus.Pending;
                entity.CreatedAt = DateTime.UtcNow;
                entity.CaseCode = GenerateCaseCode();
                entity.Street ??= string.Empty;
                entity.AgeCategoryId = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);

                if (dto.PoliceReportImage is not null)
                    entity.PoliceReportImage = await _fileStorage.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");

                // حفظ الصور وربطها بـ AI (Face Indexing)
                if (dto.Photos is not null && dto.Photos.Any())
                {
                    var photoList = dto.Photos.ToList();

                    for (int i = 0; i < photoList.Count; i++)
                    {
                        var photo = photoList[i];
                        var path = await _fileStorage.SaveFileAsync(photo, "long-term");

                        bool isPrimary = dto.PrimaryPhotoIndex == i;
                        // محاولة Index الوجه على AWS Rekognition
                        string? faceId = null;
                        try
                        {
                            faceId = await _faceRecognition.IndexFaceAsync(photo);
                        }
                        catch (Exception ex)
                        {
                            // لو فشل الـ indexing مش هنوقف العملية كلها
                            _logger.LogWarning(ex,
                                "تعذّر تسجيل الوجه في AWS للصورة {Index} أثناء إنشاء الحالة للمستخدم {UserId}.", i, userId);
                        }

                        entity.Photos.Add(new CasePhoto
                        {
                            ImagePath = path,
                            FaceId = faceId,
                            IsPrimary = isPrimary
                        });
                    }

                    // ضمان وجود صورة رئيسية واحدة بس
                    EnsureSinglePrimary(entity.Photos);
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

                    // حذف الملفات المحفوظة لو فشلت العملية
                    foreach (var photo in entity.Photos)
                        _fileStorage.DeleteFile(photo.ImagePath);

                    if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                        _fileStorage.DeleteFile(entity.PoliceReportImage);

                    _logger.LogError(ex, "فشل إنشاء الحالة للمستخدم {UserId}", userId);
                    throw;
                }
            }

            // ─────────────────────────────────────────────────────────────
            // UPDATE
            // ─────────────────────────────────────────────────────────────

            public async Task UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin)
            {
                var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                    .GetOneAsync(
                        c => c.Id == id && c.Status != CaseStatus.Deleted,
                        tracked: true,
                        c => c.Photos);

                if (entity is null)
                {
                    _logger.LogWarning("فشل التعديل - الحالة {CaseId} غير موجودة. UserId={UserId}", id, userId);
                    throw new NotFoundException($"لم يتم العثور على الحالة رقم {id}.");
                }

                if (entity.UserId != userId && !isAdmin)
                {
                    _logger.LogWarning("محاولة تعديل غير مصرح بها على الحالة {CaseId} من المستخدم {UserId}", id, userId);
                    throw new ForbiddenException("غير مسموح لك بتعديل هذه الحالة.");
                }

                if (dto.Age.HasValue)
                {
                    entity.Age = dto.Age.Value;
                    entity.AgeCategoryId = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);
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
                        _fileStorage.DeleteFile(entity.PoliceReportImage);

                    entity.PoliceReportImage = await _fileStorage.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");
                }

                // حذف الصور المطلوب حذفها مع وجوههم من AWS
                if (dto.RemovedPhotoIds is { Count: > 0 })
                {
                    var toRemove = entity.Photos.Where(p => dto.RemovedPhotoIds.Contains(p.Id)).ToList();
                    foreach (var photo in toRemove)
                    {
                        _fileStorage.DeleteFile(photo.ImagePath);

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

                        entity.Photos.Remove(photo);
                        _unitOfWork.Repository<CasePhoto>().Remove(photo);
                    }
                }

                // إضافة صور جديدة مع Face Indexing
                if (dto.NewPhotos is not null && dto.NewPhotos.Any())
                {
                    var newPhotoList = dto.NewPhotos.ToList();
                    for (int i = 0; i < newPhotoList.Count; i++)
                    {
                        var photo = newPhotoList[i];
                        var path = await _fileStorage.SaveFileAsync(photo, "long-term");

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

                        entity.Photos.Add(new CasePhoto
                        {
                            ImagePath = path,
                            FaceId = faceId,
                            IsPrimary = false
                        });
                    }
                }

                // تحديث الصورة الرئيسية لو المستخدم اختار واحدة
                if (dto.PrimaryPhotoId.HasValue)
                {
                    var targetPhoto = entity.Photos.FirstOrDefault(p => p.Id == dto.PrimaryPhotoId.Value);
                    if (targetPhoto is null)
                        throw new BadRequestException("الصورة الرئيسية المحددة غير موجودة في هذه الحالة.");

                    foreach (var p in entity.Photos)
                        p.IsPrimary = p.Id == dto.PrimaryPhotoId.Value;
                }

                // ضمان وجود صورة رئيسية دايمًا
                EnsureSinglePrimary(entity.Photos);

                if (!isAdmin && entity.Status != CaseStatus.Pending)
                {
                    entity.PreviousStatus = entity.Status;
                    entity.Status = CaseStatus.Pending;
                }

                _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation(
                    "تم تعديل الحالة {CaseId} بواسطة المستخدم {UserId} (IsAdmin={IsAdmin})", id, userId, isAdmin);
            }

            // ─────────────────────────────────────────────────────────────
            // DELETE (soft)
            // ─────────────────────────────────────────────────────────────

            public async Task DeleteAsync(long id, string userId, bool isAdmin)
            {
                var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                    .GetOneAsync(
                        c => c.Id == id && c.Status != CaseStatus.Deleted,
                        tracked: true);

                if (entity is null)
                {
                    _logger.LogWarning("فشل الحذف - الحالة {CaseId} غير موجودة. UserId={UserId}", id, userId);
                    throw new NotFoundException($"لم يتم العثور على الحالة رقم {id}.");
                }

                if (entity.UserId != userId && !isAdmin)
                {
                    _logger.LogWarning("محاولة حذف غير مصرح بها على الحالة {CaseId} من المستخدم {UserId}", id, userId);
                    throw new ForbiddenException("غير مسموح لك بحذف هذه الحالة.");
                }

                entity.PreviousStatus = entity.Status;
                entity.Status = CaseStatus.Deleted;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedByUserId = userId;

                _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
                await _unitOfWork.SaveAsync();

                _logger.LogWarning(
                    "تم الحذف المؤقت للحالة {CaseId} (كانت {PrevStatus}) بواسطة {UserId} (IsAdmin={IsAdmin})",
                    id, entity.PreviousStatus, userId, isAdmin);
            }

            // ─────────────────────────────────────────────────────────────
            // PERMANENT DELETE — Admin only
            // ─────────────────────────────────────────────────────────────

            public async Task PermanentDeleteAsync(long id)
            {
                var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                    .GetOneAsync(
                        c => c.Id == id && c.Status == CaseStatus.Deleted,
                        tracked: true,
                        c => c.Photos,
                        c => c.FoundPersonInfo!);

                if (entity is null)
                {
                    _logger.LogWarning("فشل الحذف النهائي - الحالة {CaseId} غير موجودة أو غير محذوفة مؤقتًا.", id);
                    throw new NotFoundException(
                        $"لم يتم العثور على الحالة رقم {id} أو أنها لم تُحذف مؤقتًا بعد. الحذف النهائي يتطلب حذفًا مؤقتًا أولًا.");
                }

                // حذف الوجوه من AWS وملفاتها من wwwroot
                var faceIdsToDelete = entity.Photos
                    .Where(p => !string.IsNullOrEmpty(p.FaceId))
                    .Select(p => p.FaceId!)
                    .ToList();

                if (faceIdsToDelete.Any())
                {
                    try
                    {
                        await _faceRecognition.DeleteFacesAsync(faceIdsToDelete);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "تعذّر حذف {Count} وجه من AWS عند الحذف النهائي للحالة {CaseId}.",
                            faceIdsToDelete.Count, id);
                    }
                }

                foreach (var photo in entity.Photos)
                    _fileStorage.DeleteFile(photo.ImagePath);

                if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                    _fileStorage.DeleteFile(entity.PoliceReportImage);

                _unitOfWork.Repository<LongTermMissingCase>().Remove(entity);
                await _unitOfWork.SaveAsync();

                _logger.LogWarning("تم الحذف النهائي للحالة {CaseId} بواسطة الأدمن.", id);
            }

            // ─────────────────────────────────────────────────────────────
            // APPROVE / REJECT
            // ─────────────────────────────────────────────────────────────

            public async Task ApproveAsync(long id)
            {
                var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                    .GetOneAsync(
                        c => c.Id == id && c.Status == CaseStatus.Pending,
                        tracked: true);

                if (entity is null)
                {
                    _logger.LogWarning("فشل الموافقة - الحالة {CaseId} غير موجودة أو ليست في انتظار الموافقة.", id);
                    throw new NotFoundException($"لم يتم العثور على الحالة رقم {id} أو أنها ليست في حالة انتظار.");
                }

                entity.Status = CaseStatus.Active;
                entity.PreviousStatus = null;

                _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("تمت الموافقة على الحالة {CaseId} (Pending → Active).", id);
            }

            public async Task RejectAsync(long id)
            {
                var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                    .GetOneAsync(
                        c => c.Id == id && c.Status == CaseStatus.Pending,
                        tracked: true);

                if (entity is null)
                {
                    _logger.LogWarning("فشل الرفض - الحالة {CaseId} غير موجودة أو ليست في انتظار الموافقة.", id);
                    throw new NotFoundException($"لم يتم العثور على الحالة رقم {id} أو أنها ليست في حالة انتظار.");
                }

                if (entity.PreviousStatus.HasValue)
                {
                    entity.Status = entity.PreviousStatus.Value;
                    entity.PreviousStatus = null;
                }
                else
                {
                    entity.Status = CaseStatus.Rejected;
                }

                _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("تم رفض الحالة {CaseId} (Pending → {Status}).", id, entity.Status);
            }

            // ─────────────────────────────────────────────────────────────
            // MARK AS FOUND
            // ─────────────────────────────────────────────────────────────

            public async Task MarkAsFoundedAsync(long id, MarkAsFoundedDto dto, string userId, bool isAdmin)
            {
                var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                    .GetOneAsync(
                        c => c.Id == id && c.Status == CaseStatus.Active,
                        tracked: true);

                if (entity is null)
                {
                    _logger.LogWarning(
                        "فشل تحديد الحالة كـ موجود - الحالة {CaseId} غير موجودة أو غير نشطة. UserId={UserId}", id, userId);
                    throw new NotFoundException($"لم يتم العثور على الحالة النشطة رقم {id}.");
                }

                if (entity.UserId != userId && !isAdmin)
                {
                    _logger.LogWarning(
                        "محاولة تحديد الحالة {CaseId} كـ موجود بدون صلاحية من المستخدم {UserId}", id, userId);
                    throw new ForbiddenException("غير مسموح لك بتحديد هذه الحالة كـ تم إيجاده.");
                }

                var foundInfo = new FoundPersonInfo
                {
                    CaseId = entity.Id,
                    Description = dto.Description,
                    Government = dto.Government,
                    City = dto.City,
                    Street = dto.Street ?? string.Empty,
                    FoundedAt = DateOnly.FromDateTime(DateTime.UtcNow),
                    FoundedUserId = userId
                };

                entity.Status = CaseStatus.Found;

                await _unitOfWork.Repository<FoundPersonInfo>().CreateAsync(foundInfo);
                _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("تم تحديد الحالة {CaseId} كـ موجود بواسطة المستخدم {UserId}.", id, userId);
            }

            // ─────────────────────────────────────────────────────────────
            // HELPERS
            // ─────────────────────────────────────────────────────────────

            private static string GenerateCaseCode() =>
                $"LT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

            /// <summary>
            /// يضمن وجود صورة رئيسية واحدة بالظبط.
            /// لو مفيش أي صورة معلمة كـ primary، بيعلم الأولى.
            /// لو في أكتر من واحدة، بيسيب الأولى بس.
            /// </summary>
            private static void EnsureSinglePrimary(ICollection<CasePhoto> photos)
            {
                if (!photos.Any()) return;

                var primaries = photos.Where(p => p.IsPrimary).ToList();

                if (primaries.Count == 0)
                {
                    photos.First().IsPrimary = true;
                }
                else if (primaries.Count > 1)
                {
                    foreach (var p in primaries.Skip(1))
                        p.IsPrimary = false;
                }
            }
        }
    }