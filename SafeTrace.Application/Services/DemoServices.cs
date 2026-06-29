// using System.Linq.Expressions;
// using Microsoft.EntityFrameworkCore;
// using NetTopologySuite.Geometries;
// using SafeTrace.Application.Common.Enums;
// using SafeTrace.Application.DTOs.NotificationDTOS;
// using SafeTrace.Application.DTOs.UrgentMissingCase;
// using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
// using SafeTrace.Application.DTOs.UrgentMissingCase.Response;
// using SafeTrace.Application.Helpers;
// using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;

// namespace SafeTrace.Application.Services
// {
//     internal class UrgentCaseService : IUrgentCaseService
//     {
//         private readonly ILogger<UrgentCaseService>  _logger;
//         private readonly IUnitOfWork                 _unitOfWork;
//         private readonly IMapper                     _mapper;
//         private readonly IFileStorageService         _fileStorageService;
//         private readonly INotificationServices       _notificationService;   // ← your existing service

//         private const int    RateLimitDays   = 14;
//         private const int    ExpirationHours = 48;
//         private const double NotifyRadiusM   = 50_000; // 50 km

//         public UrgentCaseService(
//             ILogger<UrgentCaseService>  logger,
//             IUnitOfWork                 unitOfWork,
//             IMapper                     mapper,
//             IFileStorageService         fileStorageService,
//             INotificationServices       notificationService)   // ← inject here
//         {
//             _logger              = logger;
//             _unitOfWork          = unitOfWork;
//             _mapper              = mapper;
//             _fileStorageService  = fileStorageService;
//             _notificationService = notificationService;
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // GET ALL  (public — userId nullable for anonymous callers)
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetAllAsync(
//             string? userId,
//             UrgentCaseFilterDto filter)
//         {
//             return await GetAllInternalAsync<UrgentCaseListItemDto>(
//                 userId,
//                 filter,
//                 includeDeleted: false,
//                 includeExpired: false,
//                 x => x.Photos,
//                 x => x.AgeCategory);
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // GET ALL  (admin — sees every status)
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<PaginationResponseDto<UrgentCaseAdminDto>>> AdminGetAllAsync(
//             UrgentCaseFilterDto filter)
//         {
//             return await GetAllInternalAsync<UrgentCaseAdminDto>(
//                 null,
//                 filter,
//                 includeDeleted: true,
//                 includeExpired: true,
//                 x => x.Photos,
//                 x => x.AgeCategory,
//                 x => x.FoundPersonInfo,
//                 x => x.User);
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // GET BY ID
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<UrgentCaseDetailDto>> GetByIdAsync(long id)
//         {
//             var entity = await _unitOfWork.Repository<UrgentCase>()
//                 .Query(tracked: false, includes: [x => x.AgeCategory, x => x.Photos, x => x.User])
//                 .FirstOrDefaultAsync(x => x.Id == id && x.Status != CaseStatus.Deleted);

//             if (entity is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Urgent case not found.");

//             return ApiResponse<UrgentCaseDetailDto>.Ok(
//                 data:    _mapper.Map<UrgentCaseDetailDto>(entity),
//                 message: "Urgent case retrieved successfully.");
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // CREATE  — FR-11 / FR-12 / FR-14 / FR-18
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<UrgentCaseDetailDto>> CreateAsync(
//             string userId,
//             UrgentCaseCreateDto createDto)
//         {
//             // ── FR-18: Rate limit (includes soft-deleted cases) ──────────
//             var rateLimitError = await CheckRateLimitAsync(userId);
//             if (rateLimitError is not null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail(rateLimitError);

//             // ── Build entity ─────────────────────────────────────────────
//             var now    = DateTime.UtcNow;
//             var entity = _mapper.Map<UrgentCase>(createDto);

//             entity.UserId         = userId;
//             entity.CaseType       = CaseType.Urgent;
//             entity.Status         = CaseStatus.Active;          // FR-12: publish immediately
//             entity.CreatedAt      = now;
//             entity.LimitReachDate = now.AddDays(RateLimitDays);
//             entity.EndDate        = now.AddHours(ExpirationHours);
//             entity.CaseCode       = Generators.GenerateCaseCode();
//             entity.AgeCategoryId  = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);
//             entity.Location       = new Point(createDto.Longitude, createDto.Latitude) { SRID = 4326 };

//             var uploadedFiles = new List<string>();

//             await _unitOfWork.BeginTransactionAsync();

//             try
//             {
//                 if (createDto.Photos?.Any() == true)
//                 {
//                     uploadedFiles = await _fileStorageService.SaveFilesAsync(createDto.Photos, "UrgentCases");

//                     entity.Photos = uploadedFiles.Select((path, index) => new CasePhoto
//                     {
//                         ImagePath = path,
//                         IsPrimary = index == 0,
//                         CreatedAt = now
//                     }).ToList();
//                 }

//                 await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);
//                 await _unitOfWork.SaveAsync();
//                 await _unitOfWork.CommitTransactionAsync();

//                 _logger.LogInformation(
//                     "Urgent case {CaseCode} created by user {UserId}. Expires: {EndDate}",
//                     entity.CaseCode, userId, entity.EndDate);
//             }
//             catch (Exception ex)
//             {
//                 await _unitOfWork.RollbackTransactionAsync();

//                 foreach (var file in uploadedFiles)
//                 {
//                     try   { _fileStorageService.DeleteFile(file); }
//                     catch (Exception deleteEx)
//                     {
//                         _logger.LogError(deleteEx,
//                             "Failed to cleanup uploaded file {FilePath}", file);
//                     }
//                 }

//                 _logger.LogError(ex, "Failed to create urgent case for user {UserId}", userId);
//                 throw;
//             }

//             // ── FR-14: Notify nearby users ───────────────────────────────
//             // Runs AFTER commit so the case is visible in the DB.
//             // Fire-and-forget — notification failure must never fail the create.
//             _ = NotifyNearbyUsersAsync(entity, createDto.Latitude, createDto.Longitude);

//             // ── Return the full created DTO ──────────────────────────────
//             var created = await _unitOfWork.Repository<UrgentCase>()
//                 .Query(tracked: false, includes: [x => x.AgeCategory, x => x.Photos, x => x.User])
//                 .FirstAsync(x => x.Id == entity.Id);

//             return ApiResponse<UrgentCaseDetailDto>.Ok(
//                 data:    _mapper.Map<UrgentCaseDetailDto>(created),
//                 message: "Urgent case created successfully.");
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // UPDATE
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<UrgentCaseDetailDto>> UpdateAsync(
//             string userId,
//             UrgentCaseUpdateDto updateDto)
//         {
//             var entity = await _unitOfWork.Repository<UrgentCase>()
//                 .GetOneAsync(
//                     x => x.Id == updateDto.Id && x.Status != CaseStatus.Deleted,
//                     tracked: true,
//                     includes: x => x.Photos);

//             if (entity is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Urgent case not found.");

//             if (entity.UserId != userId)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("You are not authorized to update this case.");

//             if (entity.Status is CaseStatus.Found or CaseStatus.Expired)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail($"Cannot update a case with status '{entity.Status}'.");

//             var uploadedFiles = new List<string>();
//             var filesToDelete = new List<string>();

//             await _unitOfWork.BeginTransactionAsync();

//             try
//             {
//                 if (updateDto.Photos?.Any() == true)
//                 {
//                     uploadedFiles = await _fileStorageService.SaveFilesAsync(updateDto.Photos, "UrgentCases");

//                     foreach (var file in uploadedFiles)
//                         entity.Photos.Add(new CasePhoto { ImagePath = file, CreatedAt = DateTime.UtcNow });
//                 }

//                 if (updateDto.DeletedPhotoIds?.Any() == true)
//                 {
//                     // SECURITY: scope deletion to photos owned by this entity only
//                     var ownedPhotoIds = entity.Photos.Select(p => p.Id).ToHashSet();
//                     var toRemove = entity.Photos
//                         .Where(p => updateDto.DeletedPhotoIds.Contains(p.Id) && ownedPhotoIds.Contains(p.Id))
//                         .ToList();

//                     filesToDelete.AddRange(toRemove.Select(p => p.ImagePath));
//                     foreach (var photo in toRemove)
//                         entity.Photos.Remove(photo);
//                 }

//                 _mapper.Map(updateDto, entity);
//                 entity.UpdatedAt = DateTime.UtcNow;

//                 if (updateDto.Age.HasValue)
//                     entity.AgeCategoryId = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);

//                 _unitOfWork.Repository<UrgentCase>().Update(entity);
//                 await _unitOfWork.SaveAsync();
//                 await _unitOfWork.CommitTransactionAsync();
//             }
//             catch (Exception ex)
//             {
//                 await _unitOfWork.RollbackTransactionAsync();

//                 foreach (var file in uploadedFiles)
//                 {
//                     try   { _fileStorageService.DeleteFile(file); }
//                     catch (Exception deleteEx)
//                     {
//                         _logger.LogError(deleteEx, "Failed to cleanup file {FilePath}", file);
//                     }
//                 }

//                 _logger.LogError(ex, "Failed to update urgent case {CaseId}", updateDto.Id);
//                 throw;
//             }

//             foreach (var file in filesToDelete)
//             {
//                 try   { _fileStorageService.DeleteFile(file); }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError(ex, "Failed to delete file {FilePath}", file);
//                 }
//             }

//             var updated = await _unitOfWork.Repository<UrgentCase>()
//                 .Query(tracked: false, includes: [x => x.AgeCategory, x => x.Photos, x => x.User])
//                 .FirstAsync(x => x.Id == entity.Id);

//             return ApiResponse<UrgentCaseDetailDto>.Ok(
//                 data:    _mapper.Map<UrgentCaseDetailDto>(updated),
//                 message: "Urgent case updated successfully.");
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // SOFT DELETE  (FR-17 — owner only)
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<string>> DeleteAsync(string userId, long id)
//         {
//             var entity = await _unitOfWork.Repository<UrgentCase>()
//                 .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted, tracked: true);

//             if (entity is null)
//                 return ApiResponse<string>.Fail("Urgent case not found.");

//             if (entity.UserId != userId)
//                 return ApiResponse<string>.Fail("You are not authorized to delete this case.");

//             if (entity.Status == CaseStatus.Found)
//                 return ApiResponse<string>.Fail("Cannot delete a case that is already marked as Found.");

//             entity.PreviousStatus  = entity.Status;
//             entity.Status          = CaseStatus.Deleted;
//             entity.DeletedAt       = DateTime.UtcNow;
//             entity.DeletedByUserId = userId;    // was missing before
//             entity.UpdatedAt       = DateTime.UtcNow;

//             await _unitOfWork.SaveAsync();

//             _logger.LogInformation("Urgent case {CaseId} soft-deleted by user {UserId}.", id, userId);

//             return ApiResponse<string>.Ok("Urgent case deleted successfully.");
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // HARD DELETE  (admin only)
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<string>> PermanentDeleteAsync(long id)
//         {
//             var entity = await _unitOfWork.Repository<UrgentCase>()
//                 .GetOneAsync(x => x.Id == id, tracked: true, includes: x => x.Photos);

//             if (entity is null)
//                 return ApiResponse<string>.Fail("Urgent case not found.");

//             if (entity.Status == CaseStatus.Found)
//                 return ApiResponse<string>.Fail("Cannot permanently delete a case marked as Founded.");

//             var filesToDelete = entity.Photos.Select(p => p.ImagePath).ToList();

//             await _unitOfWork.BeginTransactionAsync();

//             try
//             {
//                 _unitOfWork.Repository<UrgentCase>().Remove(entity);
//                 await _unitOfWork.SaveAsync();
//                 await _unitOfWork.CommitTransactionAsync();
//             }
//             catch (Exception ex)
//             {
//                 await _unitOfWork.RollbackTransactionAsync();
//                 _logger.LogError(ex, "Failed to permanently delete urgent case {CaseId}", id);
//                 throw;
//             }

//             foreach (var file in filesToDelete)
//             {
//                 try   { _fileStorageService.DeleteFile(file); }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError(ex, "Failed to delete file {FilePath} for case {CaseId}", file, id);
//                 }
//             }

//             _logger.LogInformation("Urgent case {CaseId} permanently deleted.", id);

//             return ApiResponse<string>.Ok("Urgent case permanently deleted.");
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // MARK AS FOUNDED
//         // ════════════════════════════════════════════════════════════════════
//         public async Task<ApiResponse<string>> MarkAsFoundedAsync(string userId, long id)
//         {
//             var entity = await _unitOfWork.Repository<UrgentCase>()
//                 .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted, tracked: true);

//             if (entity is null)
//                 return ApiResponse<string>.Fail("Urgent case not found.");

//             if (entity.UserId != userId)
//                 return ApiResponse<string>.Fail("You are not authorized to update this case.");

//             if (entity.Status == CaseStatus.Found)
//                 return ApiResponse<string>.Fail("Case is already marked as Found.");

//             if (entity.Status == CaseStatus.Expired)
//                 return ApiResponse<string>.Fail("Cannot mark an expired case as Found.");

//             entity.PreviousStatus = entity.Status;
//             entity.Status         = CaseStatus.Found;
//             entity.EndDate        = DateTime.UtcNow;  // close expiration window
//             entity.UpdatedAt      = DateTime.UtcNow;

//             await _unitOfWork.SaveAsync();

//             _logger.LogInformation("Urgent case {CaseId} marked as Found by user {UserId}.", id, userId);

//             return ApiResponse<string>.Ok("Case marked as Found.");
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // PRIVATE: Notify nearby users using YOUR NotificationService
//         // ════════════════════════════════════════════════════════════════════

//         /// <summary>
//         /// Queries every user whose stored location (current or home) falls within
//         /// <see cref="NotifyRadiusM"/> of the new case, then calls your existing
//         /// <see cref="INotificationServices.SendNotificationAsync"/> for each one.
//         ///
//         /// Runs fire-and-forget after the DB commit — any exception is only logged,
//         /// never propagated back to the caller.
//         /// </summary>
//         private async Task NotifyNearbyUsersAsync(
//             UrgentCase entity,
//             double caseLat,
//             double caseLng)
//         {
//             try
//             {
//                 var caseLocation = new Point(caseLng, caseLat) { SRID = 4326 };

//                 // Find all users who have a stored location within the radius.
//                 // We project to a minimal anonymous type to avoid loading the full
//                 // ApplicationUser entity for every nearby person.
//                 var nearbyUserIds = await _unitOfWork.Repository<ApplicationUser>()
//                     .Query(tracked: false)
//                     .Where(u =>
//                         u.Id != entity.UserId &&   // don't notify the creator
//                         (
//                             // current location within radius
//                             (u.CurrentLocationLatitude  != null &&
//                              u.CurrentLocationLongitude != null &&
//                              new Point(u.CurrentLocationLongitude.Value, u.CurrentLocationLatitude.Value) { SRID = 4326 }
//                                 .Distance(caseLocation) <= NotifyRadiusM)
//                             ||
//                             // fall back to home location
//                             (u.HomeLocationLatitude  != null &&
//                              u.HomeLocationLongitude != null &&
//                              new Point(u.HomeLocationLongitude.Value, u.HomeLocationLatitude.Value) { SRID = 4326 }
//                                 .Distance(caseLocation) <= NotifyRadiusM)
//                         ))
//                     .Select(u => u.Id)
//                     .ToListAsync();

//                 if (!nearbyUserIds.Any())
//                 {
//                     _logger.LogInformation(
//                         "No nearby users to notify for urgent case {CaseCode}.", entity.CaseCode);
//                     return;
//                 }

//                 _logger.LogInformation(
//                     "Notifying {Count} nearby user(s) about urgent case {CaseCode}.",
//                     nearbyUserIds.Count, entity.CaseCode);

//                 // Build the notification content once and send to each nearby user.
//                 var notificationContent =
//                     $"🚨 New urgent case nearby: {entity.CaseCode}. " +
//                     $"Missing person aged {entity.Age}, " +
//                     $"last seen in {entity.City}, {entity.Government}.";

//                 // Send in parallel — capped at 10 concurrent sends to avoid
//                 // overwhelming the hub or hitting rate limits.
//                 var semaphore = new SemaphoreSlim(10);

//                 var tasks = nearbyUserIds.Select(async recipientId =>
//                 {
//                     await semaphore.WaitAsync();
//                     try
//                     {
//                         await _notificationService.SendNotificationAsync(new SendNotificationDTO
//                         {
//                             UserId  = recipientId,
//                             Content = notificationContent,
//                             Type    = NotificationType.UrgentCase   // use whatever enum value fits
//                         });
//                     }
//                     catch (Exception ex)
//                     {
//                         // Log per-user failure — don't abort the rest
//                         _logger.LogError(ex,
//                             "Failed to notify user {UserId} about case {CaseCode}.",
//                             recipientId, entity.CaseCode);
//                     }
//                     finally
//                     {
//                         semaphore.Release();
//                     }
//                 });

//                 await Task.WhenAll(tasks);

//                 _logger.LogInformation(
//                     "Finished notifying nearby users for urgent case {CaseCode}.", entity.CaseCode);
//             }
//             catch (Exception ex)
//             {
//                 // Notification failure must NEVER propagate — the case is already saved.
//                 _logger.LogError(ex,
//                     "NotifyNearbyUsersAsync failed for urgent case {CaseCode}.", entity.CaseCode);
//             }
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // PRIVATE: Rate-limit check
//         // ════════════════════════════════════════════════════════════════════

//         /// <summary>
//         /// Returns an error message string if the user is within their 14-day window,
//         /// or null if they are allowed to create.
//         /// Intentionally includes soft-deleted cases so the window cannot be
//         /// circumvented by deleting the previous case.
//         /// </summary>
//         private async Task<string?> CheckRateLimitAsync(string userId)
//         {
//             var lastCase = await _unitOfWork.Repository<UrgentCase>()
//                 .Query(tracked: false)
//                 .Where(x => x.UserId == userId)          // all statuses — no exclusion
//                 .OrderByDescending(x => x.CreatedAt)
//                 .Select(x => new { x.CaseCode, x.Status, x.CreatedAt, x.LimitReachDate })
//                 .FirstOrDefaultAsync();

//             if (lastCase is null || lastCase.LimitReachDate <= DateTime.UtcNow)
//                 return null;

//             var remainingDays = (int)Math.Ceiling(
//                 (lastCase.LimitReachDate - DateTime.UtcNow).TotalDays);

//             _logger.LogInformation(
//                 "Rate limit hit. User {UserId} — last case {CaseCode} ({Status}), " +
//                 "LimitReachDate: {LimitReachDate}, remaining: {Days} day(s).",
//                 userId, lastCase.CaseCode, lastCase.Status,
//                 lastCase.LimitReachDate, remainingDays);

//             return $"You can create a new urgent case after {remainingDays} day(s).";
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // PRIVATE: Shared list query
//         // ════════════════════════════════════════════════════════════════════
//         private async Task<ApiResponse<PaginationResponseDto<TDto>>> GetAllInternalAsync<TDto>(
//             string? userId,
//             UrgentCaseFilterDto filter,
//             bool includeDeleted,
//             bool includeExpired,
//             params Expression<Func<UrgentCase, object>>[] includes)
//         {
//             ApplicationUser? user = null;

//             if (!string.IsNullOrWhiteSpace(userId))
//             {
//                 user = await _unitOfWork.Repository<ApplicationUser>()
//                     .Query(tracked: false)
//                     .FirstOrDefaultAsync(x => x.Id == userId);
//             }

//             var userLocation = user is not null ? GetUserLocation(user) : null;

//             var query = _unitOfWork.Repository<UrgentCase>()
//                 .Query(tracked: false, includes: includes);

//             query = ApplyFilter(query, userLocation, filter, includeDeleted, includeExpired);

//             var totalCount = await query.CountAsync();

//             query = ApplySorting(query, userLocation, filter);
//             query = ApplyPagination(query, filter);

//             var items = await query.ToListAsync();

//             var response = new PaginationResponseDto<TDto>
//             {
//                 Items      = _mapper.Map<List<TDto>>(items),
//                 PageNumber = filter.Page,
//                 PageSize   = filter.PageSize,
//                 TotalCount = totalCount
//             };

//             return ApiResponse<PaginationResponseDto<TDto>>.Ok(
//                 response,
//                 "Urgent cases retrieved successfully.");
//         }

//         // ════════════════════════════════════════════════════════════════════
//         // PRIVATE: Query helpers (unchanged from original)
//         // ════════════════════════════════════════════════════════════════════
//         private static IQueryable<UrgentCase> ApplyFilter(
//             IQueryable<UrgentCase> query,
//             Point? userLocation,
//             UrgentCaseFilterDto filter,
//             bool includeDeleted,
//             bool includeExpired)
//         {
//             if (filter.Gender.HasValue)
//                 query = query.Where(x => x.Gender == filter.Gender.Value);

//             if (filter.Status.HasValue)
//                 query = query.Where(x => x.Status == filter.Status.Value);

//             if (filter.MinAge.HasValue)
//                 query = query.Where(x => x.Age >= filter.MinAge.Value);

//             if (filter.MaxAge.HasValue)
//                 query = query.Where(x => x.Age <= filter.MaxAge.Value);

//             if (!string.IsNullOrWhiteSpace(filter.Government))
//                 query = query.Where(x => x.Government.Contains(filter.Government));

//             if (!string.IsNullOrWhiteSpace(filter.City))
//                 query = query.Where(x => x.City.Contains(filter.City));

//             if (filter.FromDate.HasValue)
//                 query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);

//             if (filter.ToDate.HasValue)
//                 query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);

//             if (!includeDeleted)
//                 query = query.Where(x => x.Status != CaseStatus.Deleted);

//             if (!includeExpired)
//                 query = query.Where(x => x.Status != CaseStatus.Expired);

//             if (userLocation != null)
//                 query = query.Where(x =>
//                     x.Location != null &&
//                     x.Location.Distance(userLocation) <= filter.RadiusInMeters);

//             return query;
//         }

//         private static IQueryable<UrgentCase> ApplySorting(
//             IQueryable<UrgentCase> query,
//             Point? userLocation,
//             UrgentCaseFilterDto filter)
//         {
//             IOrderedQueryable<UrgentCase>? ordered = null;

//             if (userLocation != null)
//                 ordered = query.OrderBy(x => x.Location!.Distance(userLocation));

//             if (filter.AgeSort.HasValue)
//             {
//                 ordered = ordered is null
//                     ? filter.AgeSort == AgeSort.Asc
//                         ? query.OrderBy(x => x.Age)
//                         : query.OrderByDescending(x => x.Age)
//                     : filter.AgeSort == AgeSort.Asc
//                         ? ordered.ThenBy(x => x.Age)
//                         : ordered.ThenByDescending(x => x.Age);
//             }

//             if (filter.DateSort.HasValue)
//             {
//                 ordered = ordered is null
//                     ? filter.DateSort == DateSort.Newest
//                         ? query.OrderByDescending(x => x.CreatedAt)
//                         : query.OrderBy(x => x.CreatedAt)
//                     : filter.DateSort == DateSort.Newest
//                         ? ordered.ThenByDescending(x => x.CreatedAt)
//                         : ordered.ThenBy(x => x.CreatedAt);
//             }

//             return ordered ?? query.OrderByDescending(x => x.CreatedAt);
//         }

//         private static IQueryable<UrgentCase> ApplyPagination(
//             IQueryable<UrgentCase> query,
//             UrgentCaseFilterDto filter)
//         {
//             filter.Page     = filter.Page     <= 0         ? 1   : filter.Page;
//             filter.PageSize = filter.PageSize <= 0         ? 10  :
//                               filter.PageSize > 100        ? 100 : filter.PageSize;

//             return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
//         }

//         private static Point? GetUserLocation(ApplicationUser? user)
//         {
//             if (user?.CurrentLocationLatitude.HasValue == true && user.CurrentLocationLongitude.HasValue)
//                 return new Point(user.CurrentLocationLongitude.Value, user.CurrentLocationLatitude.Value) { SRID = 4326 };

//             if (user?.HomeLocationLatitude.HasValue == true && user.HomeLocationLongitude.HasValue)
//                 return new Point(user.HomeLocationLongitude.Value, user.HomeLocationLatitude.Value) { SRID = 4326 };

//             return null;
//         }
//     }
// }