using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.CasesMissing.Request;
using SafeTrace.Application.DTOs.CasesMissing.Response;

namespace SafeTrace.Application.Services
{
    public class CasesService : ICasesService
    {
        private readonly ILogger<ICasesService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CasesService(ILogger<CasesService> logger, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CasesDto>> GetCases()
        {
            var data = await _unitOfWork.Repository<Case>().Query().ToListAsync();

            var dto = _mapper.Map<IEnumerable<CasesDto>>(data);

            return dto;
        }

        private async Task<ApiResponse<PaginationResponseDto<TDto>>> GetAllInternalAsync<TDto>(string? userId, FilterCasesDto filter, bool includeDeleted, bool includeExpired, params Expression<Func<Case, object>>[] includes)
        {
            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                user = await _unitOfWork.Repository<ApplicationUser>().Query(tracked: false).FirstOrDefaultAsync(x => x.Id == userId);
            }

            var userLocation = user is not null ? GetUserLocation(user) : null;    

            var query = _unitOfWork.Repository<Case>().Query(tracked: false, includes: includes);

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
        private static IQueryable<Case> ApplyFilter(IQueryable<Case> query, Point? userLocation, FilterCasesDto filter, bool includeDeleted = false, bool includeExpired = false)
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
        private static IQueryable<Case> ApplySorting(IQueryable<Case> query, Point? userLocation, FilterCasesDto filter)
        {
            IOrderedQueryable<Case>? orderedQuery = null;

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
        private static IQueryable<Case> ApplyPagination(IQueryable<Case> query, FilterCasesDto filter)
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

