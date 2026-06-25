using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
using SafeTrace.Application.DTOs.UrgentMissingCase.Response;
using SafeTrace.Application.Helpers;

namespace SafeTrace.Application.Services
{
    internal class UrgentCaseService : IUrgentCaseService
    {
        private readonly ILogger<UrgentCaseService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private const int RateLimitDays    = 14;
        private const int ExpirationHours  = 48;

        public UrgentCaseService(ILogger<UrgentCaseService> logger, IUnitOfWork unitOfWork, IMapper mapper, IFileStorageService fileStorageService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            var entity = await _unitOfWork.Repository<UrgentCase>().Query(tracked: false, includes: [x => x.AgeCategory, x => x.Photos, x => x.User])
                .FirstOrDefaultAsync(x => x.Id == id && x.Status != CaseStatus.Deleted);

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
            var lastCase = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false)
                .Where(x => x.UserId == userId && x.Status != CaseStatus.Deleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
 
            if (lastCase != null && lastCase.LimitReachDate > DateTime.UtcNow)
            {
                _logger.LogInformation(
                    "Case Id: {Id}, CaseCode: {CaseCode}, Status: {Status}, CreatedAt: {CreatedAt}, LimitReachDate: {LimitReachDate}",
                    lastCase?.Id,
                    lastCase?.CaseCode,
                    lastCase?.Status,
                    lastCase?.CreatedAt,
                    lastCase?.LimitReachDate);

                var remainingDays = (int)Math.Ceiling((lastCase.LimitReachDate - DateTime.UtcNow).TotalDays);

                return ApiResponse<UrgentCaseDetailDto>.Fail($"You can create a new urgent case after {remainingDays} day(s).");
            }
 
            var entity = _mapper.Map<UrgentCase>(createDto);

            entity.UserId         = userId;
            entity.CaseType       = CaseType.Urgent;
            entity.Status         = CaseStatus.Active;
            entity.CreatedAt      = DateTime.UtcNow;
            entity.LimitReachDate = DateTime.UtcNow.AddDays(RateLimitDays);
            entity.EndDate        = DateTime.UtcNow.AddHours(ExpirationHours);
            entity.CaseCode       = Generators.GenerateCaseCode();
            entity.AgeCategoryId  = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);

            List<string> uploadedFiles = new();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (createDto.Photos?.Any() == true)
                {
                    uploadedFiles = await _fileStorageService.SaveFilesAsync(createDto.Photos, "UrgentCases");

                    entity.Photos = uploadedFiles.Select(path => new CasePhoto
                    {
                        ImagePath = path,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();
                }

                await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);
                await _unitOfWork.SaveAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Urgent case {CaseCode} created successfully by user {UserId}.", entity.CaseCode, userId);
                
                return ApiResponse<UrgentCaseDetailDto>.Ok(message: "Urgent case created successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                foreach (var file in uploadedFiles)
                {
                    try
                    {
                        _fileStorageService.DeleteFile(file);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to cleanup uploaded file {FilePath}", file);
                    }
                }


                _logger.LogError(ex, "Failed to create urgent case for user {UserId}", userId);

                throw;
            }
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

                // Upload new photos
                if (updateDto.Photos?.Any() == true)
                {
                    uploadedFiles = await _fileStorageService.SaveFilesAsync(updateDto.Photos, "UrgentCases");

                    foreach (var file in uploadedFiles)
                    {
                        entity.Photos.Add(new CasePhoto
                        {
                            ImagePath = file,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Remove selected photos
                if (updateDto.DeletedPhotoIds?.Any() == true)
                {
                    var photos = entity.Photos.Where(x => updateDto.DeletedPhotoIds.Contains(x.Id)).ToList();

                    filesToDelete.AddRange(photos.Select(x => x.ImagePath));

                    foreach (var photo in photos)
                    {
                        entity.Photos.Remove(photo);
                    }
                }
                _unitOfWork.Repository<UrgentCase>().Update(entity);
                await _unitOfWork.SaveAsync();

                await _unitOfWork.CommitTransactionAsync();

                // Delete physical files after successful commit
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        _fileStorageService.DeleteFile(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete file {FilePath} after updating urgent case {CaseId}", file, entity.Id);
                    }
                }

                _logger.LogInformation("Urgent case {CaseId} updated successfully by user {UserId}.", entity.Id, userId);

                return ApiResponse<UrgentCaseDetailDto>.Ok(message: "Urgent case updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                // Remove uploaded files if DB operation fails
                foreach (var file in uploadedFiles)
                {
                    try
                    {
                        _fileStorageService.DeleteFile(file);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to cleanup uploaded file {FilePath}", file);
                    }
                }

                _logger.LogError(ex, "Failed to update urgent case {CaseId} for user {UserId}", updateDto.Id, userId);

                throw;
            }
        }
        public async Task<ApiResponse<string>> DeleteAsync(string userId, long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>()
                .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted, tracked: true);
 
            if (entity is null)
                return ApiResponse<string>.Fail("Urgent case not found.");

            if (entity.UserId != userId)
                return ApiResponse<string>.Fail("You are not authorized to delete this case.");

            if (entity.Status == CaseStatus.Found)
                return ApiResponse<string>.Fail("Cannot delete a case that is already marked as Found.");
 
            entity.PreviousStatus = entity.Status;
            entity.Status = CaseStatus.Deleted;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;      
 
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseId} soft-deleted by user {UserId}", id, userId); 

            return ApiResponse<string>.Ok("Urgent case deleted successfully.");
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

            // Delete physical files AFTER successful commit
            foreach (var file in filesToDelete)
            {
                try
                {
                    _fileStorageService.DeleteFile(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file {FilePath} for urgent case {CaseId}", file, id);
                }
            }
 
            _logger.LogInformation("Urgent case {CaseId} permanently deleted.", id);
 
            return ApiResponse<string>.Ok("Urgent case permanently deleted.");
        }


        private async Task<ApiResponse<PaginationResponseDto<TDto>>> GetAllInternalAsync<TDto>(string? userId, UrgentCaseFilterDto filter, bool includeDeleted, bool includeExpired, params Expression<Func<UrgentCase, object>>[] includes)
        {
            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                user = await _unitOfWork.Repository<ApplicationUser>().Query(tracked: false).FirstOrDefaultAsync(x => x.Id == userId);
            }

            var userLocation = user is not null ? GetUserLocation(user) : null;    

            var query = _unitOfWork.Repository<UrgentCase>().Query(tracked: false, includes: includes);

            query = ApplyFilter(query, userLocation, filter, includeDeleted, includeExpired);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, userLocation, filter);

            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            var response = new PaginationResponseDto<TDto>
            {
                Items = _mapper.Map<List<TDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<TDto>>.Ok(
                response,
                "Urgent cases retrieved successfully");
        }
        private static IQueryable<UrgentCase> ApplyFilter(IQueryable<UrgentCase> query, Point? userLocation, UrgentCaseFilterDto filter, bool includeDeleted = false, bool includeExpired = false)
        {
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

            #region Deleted & Expired

            if (!includeDeleted)
                query = query.Where(x => x.Status != CaseStatus.Deleted);

            if (!includeExpired)
                query = query.Where(x => x.Status != CaseStatus.Expired);

            #endregion

            #region Location

            if (userLocation != null)
            {
                query = query.Where(x => x.Location != null && x.Location.Distance(userLocation) <= filter.RadiusInMeters);
            }

            #endregion

            return query;
        }
        private static IQueryable<UrgentCase> ApplySorting(IQueryable<UrgentCase> query, Point? userLocation, UrgentCaseFilterDto filter)
        {
            IOrderedQueryable<UrgentCase>? orderedQuery = null;

            // Distance Sort
            if (userLocation != null)
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

            return orderedQuery ?? query.OrderByDescending(x => x.CreatedAt);
        }
        private static IQueryable<UrgentCase> ApplyPagination(IQueryable<UrgentCase> query, UrgentCaseFilterDto filter)
        {
            filter.Page = filter.Page <= 0 ? 1 : filter.Page;

            filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

            return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
        }
        private static Point? GetUserLocation(ApplicationUser? user)
        {
            if (user?.CurrentLocationLatitude.HasValue == true && user.CurrentLocationLongitude.HasValue)
            {
                return new Point(user.CurrentLocationLongitude.Value, user.CurrentLocationLatitude.Value)
                {
                    SRID = 4326
                };
            }

            if (user?.HomeLocationLatitude.HasValue == true && user.HomeLocationLongitude.HasValue)
            {
                return new Point(user.HomeLocationLongitude.Value, user.HomeLocationLatitude.Value)
                {
                    SRID = 4326
                };
            }

            return null;
        }
    }
}