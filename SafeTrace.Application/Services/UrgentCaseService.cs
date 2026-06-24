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
        private const int RateLimitDays    = 14;
        private const int ExpirationHours  = 48;

        public UrgentCaseService(ILogger<UrgentCaseService> logger, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<ApiResponse<PaginationResponseDto<UrgentCaseAdminDto>>> AdminGetAllAsync(string userId, UrgentCaseFilterDto filter)
        {
            return await GetAllInternalAsync<UrgentCaseAdminDto>(
                userId,
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

            // ── Rate-limit: one urgent case per 14 days ───────────────────────
            var cutoff = DateTime.UtcNow.AddDays(RateLimitDays);
 
            bool hasRecent = await _unitOfWork.Repository<UrgentCase>().Query(tracked: false)
                .AnyAsync(x => x.UserId == userId && x.Status != CaseStatus.Deleted && x.CreatedAt >= cutoff);
 
            if (hasRecent)
            {
                _logger.LogWarning("User {UserId} hit the {Days}-day rate limit for urgent case creation.", userId, RateLimitDays);
 
                return ApiResponse<UrgentCaseDetailDto>.Fail($"You can only create one urgent case every {RateLimitDays} days.");
            }
 
            var entity = _mapper.Map<UrgentCase>(createDto);
 
            entity.CaseType       = CaseType.Urgent;
            entity.Status         = CaseStatus.Active;
            entity.CreatedAt      = DateTime.UtcNow;
            entity.LimitReachDate = DateTime.UtcNow.AddDays(RateLimitDays);
            entity.EndDate        = DateTime.UtcNow.AddHours(ExpirationHours);
            entity.CaseCode       = Generators.GenerateCaseCode();
 
            await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseCode} created by user {UserId}.", entity.CaseCode, entity.UserId);
 
            return ApiResponse<UrgentCaseDetailDto>.Ok(message: "Urgent case created successfully.");
        }
        

        public async Task<ApiResponse<UrgentCaseDetailDto>> UpdateAsync(string userId, UrgentCaseUpdateDto updateDto)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>()
                .GetOneAsync(x => x.Id == updateDto.Id && x.Status != CaseStatus.Deleted, tracked: true);
 
            if (entity is null)
                return ApiResponse<UrgentCaseDetailDto>.Fail("Urgent case not found.");
 
            if (entity.UserId != userId)
                return ApiResponse<UrgentCaseDetailDto>.Fail("You are not authorized to update this case.");
 
            if (entity.Status is CaseStatus.Found or CaseStatus.Expired)
                return ApiResponse<UrgentCaseDetailDto>.Fail($"Cannot update a case with status '{entity.Status}'.");
 
            _mapper.Map(updateDto, entity);
            entity.UpdatedAt = DateTime.UtcNow;  
 
            _unitOfWork.Repository<UrgentCase>().Update(entity);
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseId} updated by user {UserId}.", entity.Id, updateDto.UserId);
 
            return ApiResponse<UrgentCaseDetailDto>.Ok(message: "Urgent case updated successfully.");
        }

        public async Task<ApiResponse<string>> DeleteAsync(long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>()
                .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted, tracked: true);
 
            if (entity is null)
                return ApiResponse<string>.Fail("Urgent case not found.");
 
            if (entity.Status == CaseStatus.Found)
                return ApiResponse<string>.Fail("Cannot delete a case that is already marked as Founded.");
 
            entity.PreviousStatus = entity.Status;
            entity.Status         = CaseStatus.Deleted;
            entity.DeletedAt      = DateTime.UtcNow;         
 
            _unitOfWork.Repository<UrgentCase>().Update(entity);
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseId} soft-deleted.", id);
 
            return ApiResponse<string>.Ok("Urgent case deleted successfully.");
        }

        public async Task<ApiResponse<string>> MarkAsFoundedAsync(long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>().GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted, tracked: true);
 
            if (entity is null)
                return ApiResponse<string>.Fail("Urgent case not found.");
 
            if (entity.Status == CaseStatus.Found)
                return ApiResponse<string>.Fail("Case is already marked as Founded.");
 
            entity.PreviousStatus = entity.Status;
            entity.Status         = CaseStatus.Found;
            entity.EndDate        = DateTime.UtcNow;
 
            _unitOfWork.Repository<UrgentCase>().Update(entity);
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseId} marked as Founded.", id);
 
            return ApiResponse<string>.Ok("Case marked as Founded.");
        }

        public async Task<ApiResponse<string>> PermanentDeleteAsync(long id)
        {
            var entity = await _unitOfWork.Repository<UrgentCase>()
                .GetOneAsync(x => x.Id == id, tracked: true);
 
            if (entity is null)
                return ApiResponse<string>.Fail("Urgent case not found.");
 
            if (entity.Status == CaseStatus.Found)
                return ApiResponse<string>.Fail("Cannot permanently delete a case marked as Founded.");
 
            _unitOfWork.Repository<UrgentCase>().Remove(entity);
            await _unitOfWork.SaveAsync();
 
            _logger.LogInformation("Urgent case {CaseId} permanently deleted.", id);
 
            return ApiResponse<string>.Ok("Urgent case permanently deleted.");
        }

        private async Task<ApiResponse<PaginationResponseDto<TDto>>> GetAllInternalAsync<TDto>(string userId, UrgentCaseFilterDto filter, bool includeDeleted, bool includeExpired, params Expression<Func<UrgentCase, object>>[] includes)
        {
            var query = _unitOfWork.Repository<UrgentCase>().Query(tracked: false, includes: includes);

            query = ApplyFilter(query, userId, filter, includeDeleted, includeExpired);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, userId, filter);
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
        private IQueryable<UrgentCase> ApplyFilter(IQueryable<UrgentCase> query, string userId, UrgentCaseFilterDto filter, bool includeDeleted = false, bool includeExpired = false)
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

            var user = _unitOfWork.Repository<ApplicationUser>().Query().FirstOrDefault(x => x.Id == userId);


            Point? userLocation = null;

            if (user?.CurrentLocationLatitude.HasValue == true && user.CurrentLocationLongitude.HasValue)
            {
                userLocation = new Point(user.CurrentLocationLongitude.Value, user.CurrentLocationLatitude.Value)
                {
                    SRID = 4326
                };
            }
            else if (user?.HomeLocationLatitude.HasValue == true && user.HomeLocationLongitude.HasValue)
            {
                userLocation = new Point(user.HomeLocationLongitude.Value, user.HomeLocationLatitude.Value)
                {
                    SRID = 4326
                };
            }

            if (userLocation != null)
            {
                query = query.Where(x => x.Location != null && x.Location.Distance(userLocation) <= filter.RadiusInMeters);
            }

            #endregion

            return query;
        }
        private IQueryable<UrgentCase> ApplySorting(IQueryable<UrgentCase> query, string userId, UrgentCaseFilterDto filter)
        {
            IOrderedQueryable<UrgentCase>? orderedQuery = null;

            var user =  _unitOfWork.Repository<ApplicationUser>().Query().Where(x => x.Id == userId).FirstOrDefault();


            // Distance Sort
            Point? userLocation = null;

            if (user.CurrentLocationLatitude.HasValue && user.CurrentLocationLongitude.HasValue)
            {
                userLocation = new Point(user.CurrentLocationLongitude.Value, user.CurrentLocationLatitude.Value)
                {
                    SRID = 4326
                };
            }
            else if (user.HomeLocationLatitude.HasValue && user.HomeLocationLongitude.HasValue)
            {
                userLocation = new Point(user.HomeLocationLongitude.Value, user.HomeLocationLatitude.Value)
                {
                    SRID = 4326
                };
            }

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
            return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
        }

    }
}