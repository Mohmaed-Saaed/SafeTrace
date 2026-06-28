using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
using SafeTrace.Application.DTOs.UrgentMissingCase.Response;
using SafeTrace.Application.Helpers;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;

namespace SafeTrace.Application.Services
{
    internal class UrgentCaseService : IUrgentCaseService
    {
        private readonly ILogger<UrgentCaseService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationServices _notificationServices;
        private readonly IFileStorageService _fileStorageService;
        private const int RateLimitDays    = 14;
        private const int ExpirationHours  = 48;

        public UrgentCaseService(ILogger<UrgentCaseService> logger, IUnitOfWork unitOfWork, IMapper mapper, INotificationServices notificationServices, IFileStorageService fileStorageService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationServices = notificationServices;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetAllAsync(string userId, UrgentCaseFilterDto filter)
        {
            return await GetAllInternalAsync<UrgentCaseListItemDto>(
                userId,
                filter,
                includeDeleted: false,
                includeExpired: false,
                x => x.Photos,
                x => x.AgeCategory);
        }
        public async Task<ApiResponse<PaginationResponseDto<UrgentCaseAdminDto>>> AdminGetAllAsync(UrgentCaseFilterDto filter)
        {
            return await GetAllInternalAsync<UrgentCaseAdminDto>(
                null,
                filter,
                includeDeleted: true,
                includeExpired: true,
                x => x.Photos,
                x => x.AgeCategory,
                x => x.FoundPersonInfo
            );
        }
        public async Task<ApiResponse<UrgentCaseDetailDto>> GetByIdAsync(long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false, includes: [x => x.AgeCategory, x => x.Photos, x => x.User])
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return ApiResponse<UrgentCaseDetailDto>.Fail(message: "Urgent case not found");

            var dto = _mapper.Map<UrgentCaseDetailDto>(entity);

            return ApiResponse<UrgentCaseDetailDto>.Ok(
                data: dto,
                message: "Urgent case retrieved successfully"
            );
        }
        public async Task<ApiResponse<UrgentCaseDetailDto>> CreateAsync(string userId, UrgentCaseCreateDto createDto)
        {
            // ── FR-18 Rate limit ─────────────────────────────────────────
            // Intentionally includes soft-deleted cases so users cannot bypass
            // the 14-day window by deleting their previous case.
            var rateLimitViolation = await CheckRateLimitAsync(userId);
            if (rateLimitViolation is not null)
                return ApiResponse<UrgentCaseDetailDto>.Fail(rateLimitViolation);

            // ── Build entity ─────────────────────────────────────────────
            var now    = DateTime.UtcNow;
            var entity = _mapper.Map<UrgentCase>(createDto);

            entity.UserId         = userId;
            entity.CaseType       = CaseType.Urgent;
            entity.Status         = CaseStatus.Active;
            entity.CreatedAt      = now;
            entity.LimitReachDate = now.AddDays(RateLimitDays);
            entity.EndDate        = now.AddHours(ExpirationHours);
            entity.CaseCode       = Generators.GenerateCaseCode();
            entity.AgeCategoryId  = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);
            entity.Location       = new Point(createDto.Longitude, createDto.Latitude) { SRID = 4326 };

            // ── Persist with file handling ────────────────────────────────
            var uploadedFiles = new List<string>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (createDto.Photos?.Count > 0)
                {
                    uploadedFiles = await _fileStorageService.SaveFilesAsync(createDto.Photos, "UrgentCases");

                    entity.Photos = uploadedFiles
                        .Select((path, index) => new CasePhoto
                        {
                            ImagePath = path,
                            IsPrimary = index == 0,   // first uploaded photo is primary
                            CreatedAt = now
                        })
                        .ToList();
                }

                await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Urgent case {CaseCode} created by user {UserId}. EndDate: {EndDate}",
                    entity.CaseCode, userId, entity.EndDate);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await CleanupUploadedFilesAsync(uploadedFiles);

                _logger.LogError(ex, "Failed to create urgent case for user {UserId}", userId);

                throw;
            }

            // // ── FR-14 Notify nearby users ─────────────────────────────────
            // // Fire-and-forget — notification failure must not affect the response.
            // _ = _notificationServices.NotifyNearbyUsersAsync(new NewUrgentCaseNotification(
            //         CaseId:        entity.Id,
            //         CaseCode:      entity.CaseCode,
            //         Latitude:      createDto.Latitude,
            //         Longitude:     createDto.Longitude,
            //         RadiusInMeters: DefaultRadiusM,
            //         Age:           entity.Age,
            //         Gender:        entity.Gender.ToString(),
            //         Government:    entity.Government,
            //         City:          entity.City),
            //     CancellationToken.None);    // separate token — don't cancel on request end

            // ── Return populated DTO ──────────────────────────────────────
            var created = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false, includes:
                [
                    x => x.AgeCategory,
                    x => x.Photos,
                    x => x.User
                ])
                .FirstAsync(x => x.Id == entity.Id);

            return ApiResponse<UrgentCaseDetailDto>.Ok(
                data: _mapper.Map<UrgentCaseDetailDto>(created),
                message: "Urgent case created successfully.");
        }
        public async Task<ApiResponse<UrgentCaseDetailDto>> UpdateAsync(string userId, UrgentCaseUpdateDto updateDto)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>().GetOneAsync(x => x.Id == updateDto.Id && x.Status != CaseStatus.Deleted, tracked: true, includes: x => x.Photos);

            if (entity is null)
                return ApiResponse<UrgentCaseDetailDto>.Fail("Urgent case not found.");

            if (entity.UserId != userId)
                return ApiResponse<UrgentCaseDetailDto>.Fail("You are not authorized to update this case.");

            if (entity.Status is CaseStatus.Found or CaseStatus.Expired)
                return ApiResponse<UrgentCaseDetailDto>.Fail($"Cannot update a case with status '{entity.Status}'.");

            var uploadedFiles = new List<string>();
            var filesToDelete = new List<string>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // ── New photos ───────────────────────────────────────────
                if (updateDto.Photos?.Count > 0)
                {
                    uploadedFiles = await _fileStorageService.SaveFilesAsync(updateDto.Photos, "UrgentCases");

                    foreach (var path in uploadedFiles)
                    {
                        entity.Photos.Add(new CasePhoto
                        {
                            ImagePath = path,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // ── Remove selected photos ───────────────────────────────
                // SECURITY: only remove photos that belong to THIS entity.
                if (updateDto.DeletedPhotoIds?.Count > 0)
                {
                    var ownedIds = entity.Photos.Select(p => p.Id).ToHashSet();
                    var toRemove = entity.Photos.Where(p => updateDto.DeletedPhotoIds.Contains(p.Id) && ownedIds.Contains(p.Id)).ToList();

                    filesToDelete.AddRange(toRemove.Select(p => p.ImagePath));

                    foreach (var photo in toRemove)
                    {
                        entity.Photos.Remove(photo);
                    }
                }

                // ── Map scalar properties ────────────────────────────────
                _mapper.Map(updateDto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                // Re-resolve age category if age changed
                if (updateDto.Age.HasValue)
                    entity.AgeCategoryId = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);

                _unitOfWork.Repository<UrgentCase>().Update(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Urgent case {CaseId} updated by user {UserId}.", entity.Id, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await CleanupUploadedFilesAsync(uploadedFiles);

                _logger.LogError(ex, "Failed to update urgent case {CaseId} for user {UserId}", updateDto.Id, userId);

                throw;
            }

            // Delete physical files after successful commit
            await CleanupUploadedFilesAsync(filesToDelete);

            // ── Reload and return ────────────────────────────────────────
            var updated = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false, includes:
                [
                    x => x.AgeCategory,
                    x => x.Photos,
                    x => x.User
                ])
                .FirstAsync(x => x.Id == entity.Id);

            return ApiResponse<UrgentCaseDetailDto>.Ok(
                data: _mapper.Map<UrgentCaseDetailDto>(updated),
                message: "Urgent case updated successfully.");
        }
        public async Task<ApiResponse<string>> DeleteAsync(string userId, long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>().GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted, tracked: true);
 
            if (entity is null)
                return ApiResponse<string>.Fail("Urgent case not found.");

            if (entity.UserId != userId)
                return ApiResponse<string>.Fail("You are not authorized to delete this case.");

            if (entity.Status == CaseStatus.Found)
                return ApiResponse<string>.Fail("Cannot delete a case that is already marked as Found.");
 
            entity.PreviousStatus = entity.Status;
            entity.Status = CaseStatus.Deleted;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId   = userId;
            entity.UpdatedAt = DateTime.UtcNow;      
 
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseId} soft-deleted by user {UserId}", id, userId); 

            return ApiResponse<string>.Ok("Urgent case deleted successfully.");
        }
        public async Task<ApiResponse<string>> PermanentDeleteAsync(long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>().GetOneAsync(x => x.Id == id, tracked: true, includes: x => x.Photos);
 
            if (entity is null)
                return ApiResponse<string>.Fail("Urgent case not found.");
 
            if (entity.Status == CaseStatus.Found)
                return ApiResponse<string>.Fail("Cannot permanently delete a case marked as Founded.");
 
            var filesToDelete = entity.Photos.Select(x => x.ImagePath).ToList();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                _unitOfWork.Repository<UrgentCase>().Remove(entity);

                await _unitOfWork.SaveAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Failed to permanently delete urgent case {CaseId}", id);

                throw;
            }

            // Delete physical files AFTER successful DB commit
            await CleanupUploadedFilesAsync(filesToDelete);
 
            _logger.LogInformation("Urgent case {CaseId} permanently deleted.", id);
 
            return ApiResponse<string>.Ok("Urgent case permanently deleted.");
        }
        public async Task<ApiResponse<string>> MarkAsFoundedAsync(string userId, long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>().GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted, tracked: true);
 
            if (entity is null)
                return ApiResponse<string>.Fail("Urgent case not found.");
            
            if (entity.UserId != userId)
                return ApiResponse<string>.Fail("You are not authorized to update this case.");
 
            if (entity.Status == CaseStatus.Found)
                return ApiResponse<string>.Fail("Case is already marked as Found.");
            
            if (entity.Status == CaseStatus.Expired)
                return ApiResponse<string>.Fail("Cannot mark an expired case as found.");
 
            entity.PreviousStatus = entity.Status;
            entity.Status = CaseStatus.Found;
            entity.EndDate = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
             
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseId} marked as Found by user {UserId}.", id, userId); 

            return ApiResponse<string>.Ok("Case marked as Found.");
        }
        
        private async Task<ApiResponse<PaginationResponseDto<TDto>>> GetAllInternalAsync<TDto>(string? userId, UrgentCaseFilterDto filter, bool includeDeleted, bool includeExpired, params Expression<Func<UrgentCase, object>>[] includes)
        {
            // Resolve optional user location for proximity sorting / radius filtering
            Point? userLocation = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var user = await _unitOfWork.Repository<ApplicationUser>()
                    .Query(tracked: false)
                    .Select(u => new
                    {
                        u.Id,
                        u.CurrentLocationLatitude,
                        u.CurrentLocationLongitude,
                        u.HomeLocationLatitude,
                        u.HomeLocationLongitude
                    })
                    .FirstOrDefaultAsync(u => u.Id == userId);

                userLocation = ResolveUserLocation(user);
            }

            var query = _unitOfWork.Repository<UrgentCase>().Query(tracked: false, includes: includes);

            query = ApplyFilter(query, userLocation, filter, includeDeleted, includeExpired);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, userLocation, filter);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            var response = new PaginationResponseDto<TDto>
            {
                Items      = _mapper.Map<List<TDto>>(items),
                PageNumber = filter.Page,
                PageSize   = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<TDto>>.Ok(
                response,
                "Urgent cases retrieved successfully.");
        }
        private static IQueryable<UrgentCase> ApplyFilter(IQueryable<UrgentCase> query, Point? userLocation, UrgentCaseFilterDto filter, bool includeDeleted = false, bool includeExpired = false)
        {

            #region Deleted & Expired

            if (!includeDeleted)
                query = query.Where(x => x.Status != CaseStatus.Deleted);

            if (!includeExpired)
                query = query.Where(x => x.Status != CaseStatus.Expired);

            #endregion

            #region Basic Filters

            if (filter.Gender.HasValue)
                query = query.Where(x => x.Gender == filter.Gender.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.MinAge.HasValue)
                query = query.Where(x => x.Age >= filter.MinAge.Value);

            if (filter.MaxAge.HasValue)
                query = query.Where(x => x.Age <= filter.MaxAge.Value);

            if (!string.IsNullOrWhiteSpace(filter.Government))
                query = query.Where(x => x.Government.Contains(filter.Government));

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(x => x.City.Contains(filter.City));

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);

            #endregion

            #region Location

            if (userLocation is not null)
            {
                query = query.Where(x => x.Location != null && x.Location.Distance(userLocation) <= filter.RadiusInMeters);
            }

            #endregion

            return query;
        }
        private static IQueryable<UrgentCase> ApplySorting(IQueryable<UrgentCase> query, Point? userLocation, UrgentCaseFilterDto filter)
        {
            IOrderedQueryable<UrgentCase>? orderedQuery = null;

            // Primary sort: proximity (when location is available)
            if (userLocation is not null)
            {
                orderedQuery = query.OrderBy(x => x.Location.Distance(userLocation));
            }

            // Age Sort
            if (filter.AgeSort.HasValue)
            {
                orderedQuery = orderedQuery == null
                    ? (filter.AgeSort == AgeSort.Asc
                        ? query.OrderBy(x => x.Age)
                        : query.OrderByDescending(x => x.Age))
                    : (filter.AgeSort == AgeSort.Asc
                        ? orderedQuery.ThenBy(x => x.Age)
                        : orderedQuery.ThenByDescending(x => x.Age));
            }

            // Date Sort
            if (filter.DateSort.HasValue)
            {
                orderedQuery = orderedQuery == null
                    ? (filter.DateSort == DateSort.Newest
                        ? query.OrderByDescending(x => x.CreatedAt)
                        : query.OrderBy(x => x.CreatedAt))
                    : (filter.DateSort == DateSort.Newest
                        ? orderedQuery.ThenByDescending(x => x.CreatedAt)
                        : orderedQuery.ThenBy(x => x.CreatedAt));
            }

            // Default: newest first
            return orderedQuery ?? query.OrderByDescending(x => x.CreatedAt);
        }
        private static IQueryable<UrgentCase> ApplyPagination(IQueryable<UrgentCase> query, UrgentCaseFilterDto filter)
        {
            filter.Page = filter.Page <= 0 ? 1 : filter.Page;

            filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

            return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
        }  
        private async Task<string?> CheckRateLimitAsync(string userId)
        {
            var lastCase = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.CaseCode, x.Status, x.CreatedAt, x.LimitReachDate })
                .FirstOrDefaultAsync();

            if (lastCase is null || lastCase.LimitReachDate <= DateTime.UtcNow)
                return null;

            var remaining = (int)Math.Ceiling((lastCase.LimitReachDate - DateTime.UtcNow).TotalDays);

            _logger.LogInformation(
                "Rate limit hit for user {UserId}. Last case: {CaseCode} ({Status}). " +
                "LimitReachDate: {LimitReachDate}. Remaining: {Days} day(s).",
                userId, lastCase.CaseCode, lastCase.Status,
                lastCase.LimitReachDate, remaining);

            return $"You can create a new urgent case in {remaining} day(s).";
        }
        private static Point? ResolveUserLocation(dynamic? user)
        {
            if (user is null) return null;

            if (user.CurrentLocationLatitude is not null && user.CurrentLocationLongitude is not null)
            {
                return new Point((double)user.CurrentLocationLongitude, (double)user.CurrentLocationLatitude) { SRID = 4326 };
            }

            if (user.HomeLocationLatitude is not null && user.HomeLocationLongitude is not null)
            {
                return new Point((double)user.HomeLocationLongitude, (double)user.HomeLocationLatitude) { SRID = 4326 };
            }
            return null;
        }
        private async Task CleanupUploadedFilesAsync(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                try
                {
                    _fileStorageService.DeleteFile(path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete physical file {FilePath}", path);
                }
            }

            await Task.CompletedTask; // keeps the signature async for future blob storage
        }
    }
}