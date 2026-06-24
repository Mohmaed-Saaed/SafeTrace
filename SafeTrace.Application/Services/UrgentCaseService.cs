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

        // private readonly ICurrentUserService _currentUserService;

        public UrgentCaseService(ILogger<UrgentCaseService> logger, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetAllAsync(UrgentCaseFilterDto filter)
        {
            var query = _unitOfWork.Repository<UrgentCase>().Query(tracked: false, includes: [x => x.Photos, x => x.AgeCategory]);

            query = ApplyFilter(query, filter, includeDeleted: false, includeExpired: false);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter);

            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            var data = _mapper.Map<List<UrgentCaseListItemDto>>(items);

            var response = new PaginationResponseDto<UrgentCaseListItemDto>
            {
                Items = data,
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>.Ok(
                data: response,
                message: "Urgent cases retrieved successfully"
            );
        }
        public async Task<ApiResponse<PaginationResponseDto<UrgentCaseAdminDto>>> AdminGetAllAsync(UrgentCaseFilterDto filter)
        {
            var query = _unitOfWork.Repository<UrgentCase>().Query(tracked: false, includes: [x => x.Photos, x => x.AgeCategory, x =>  x.FoundPersonInfo]);
            
            query = ApplyFilter(query, filter, includeDeleted: true, includeExpired: true);
            
            query = ApplySorting(query, filter);

            query = ApplyPagination(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query.ToListAsync();

            var data = _mapper.Map<List<UrgentCaseAdminDto>>(items);

            var response = new PaginationResponseDto<UrgentCaseAdminDto>
            {
                Items = data,
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<UrgentCaseAdminDto>>.Ok(
                data: response,
                message: "Urgent cases retrieved successfully"
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

        public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetMyCasesAsync(UrgentCaseFilterDto filter)
        {
            var userId = filter.UserId;

            var query = _unitOfWork.Repository<UrgentCase>().Query(tracked: false, includes: [x => x.Photos, x => x.AgeCategory]);

            query = ApplyFilter(query, filter, includeDeleted: false, includeExpired: false).Where(x => x.UserId == userId);

            query = ApplySorting(query, filter);

            var totalCount = await query.CountAsync();

            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            var data = _mapper.Map<List<UrgentCaseListItemDto>>(items);

            var response = new PaginationResponseDto<UrgentCaseListItemDto>
            {
                Items = data,
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>.Ok(
                data: response,
                message: "Your urgent cases retrieved successfully"
            );
        }

        public async Task<ApiResponse<UrgentCaseDetailDto>> CreateAsync(UrgentCaseCreateDto createDto)
        {
            var userId = createDto.UserId;

            var lastUrgent = await _unitOfWork.Repository<UrgentCase>().Query(tracked: false)
                .Where(x => x.UserId == userId && x.Status != CaseStatus.Deleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastUrgent != null && (DateTime.UtcNow - lastUrgent.CreatedAt).TotalDays < 14)
            {
                return ApiResponse<UrgentCaseDetailDto>.Fail(message: "You can only create one urgent case every 14 days");
            }

            var entity = _mapper.Map<UrgentCase>(createDto);

            entity.UserId = userId;
            entity.CaseType = CaseType.Urgent;
            entity.Status = CaseStatus.Active;
            entity.CreatedAt = DateTime.UtcNow;
            entity.LimitReachDate = DateTime.UtcNow.AddHours(48);
            entity.CaseCode = Generators.GenerateCaseCode();

            await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);
            await _unitOfWork.SaveAsync();

            var dto = _mapper.Map<UrgentCaseDetailDto>(entity);

            return ApiResponse<UrgentCaseDetailDto>.Ok(
                data: dto,
                message: "Urgent case created successfully");
        }

        public async Task<ApiResponse<UrgentCaseDetailDto>> UpdateAsync(UrgentCaseUpdateDto updateDto)
        {
            var userId = updateDto;
            // var isAdmin = _currentUserService.IsAdmin();

            var entity = await _unitOfWork.Repository<UrgentCase>()
                .GetOneAsync(x => x.Id == updateDto.Id && x.Status != CaseStatus.Deleted);

            if (entity == null)
                return new ApiResponse<UrgentCaseDetailDto>
                {
                    Success = false,
                    Message = "Urgent case not found"
                };

            // Only owner or admin can update
            // if (entity.UserId != userId && !isAdmin)
            //     return new ApiResponse<UrgentCaseDetailDto>
            //     {
            //         Success = false,
            //         Message = "You are not authorized to update this case"
            //     };

            // Cannot update a found or expired case? (business decision)
            if (entity.Status == CaseStatus.Found || entity.Status == CaseStatus.Expired)
                return new ApiResponse<UrgentCaseDetailDto>
                {
                    Success = false,
                    Message = $"Cannot update a case that is {entity.Status}"
                };

            _mapper.Map(updateDto, entity);
            entity.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<UrgentCase>().Update(entity);
            await _unitOfWork.SaveAsync();

            var dto = _mapper.Map<UrgentCaseDetailDto>(entity);
            return new ApiResponse<UrgentCaseDetailDto>
            {
                Success = true,
                Data = dto,
                Message = "Urgent case updated successfully"
            };
        }

        // // ------------------------- SOFT DELETE -------------------------
        // public async Task<ApiResponse<string>> DeleteAsync(long id)
        // {
        //     // var userId = _currentUserService.GetUserId();
        //     // var isAdmin = _currentUserService.IsAdmin();

        //     var entity = await _unitOfWork.Repository<UrgentCase>()
        //         .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted);

        //     if (entity == null)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Urgent case not found"
        //         };

        //     // Only owner or admin can delete
        //     // if (entity.UserId != userId && !isAdmin)
        //     //     return new ApiResponse<string>
        //     //     {
        //     //         Success = false,
        //     //         Message = "You are not authorized to delete this case"
        //     //     };

        //     // FR‑16: Cannot delete if already marked as Found
        //     if (entity.Status == CaseStatus.Found)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Cannot delete a case that is already marked as Founded"
        //         };

        //     // Soft delete: set status to Deleted
        //     entity.Status = CaseStatus.Deleted;
        //     entity.DeletedAt = DateTime.Now;
        //     // entity.DeletedByUserId = userId;

        //     _unitOfWork.Repository<UrgentCase>().Update(entity);
        //     await _unitOfWork.SaveAsync();

        //     return new ApiResponse<string>
        //     {
        //         Success = true,
        //         Message = "Urgent case deleted successfully"
        //     };
        // }

        // // ------------------------- PERMANENT DELETE (hard delete) -------------------------
        // public async Task<ApiResponse<string>> PermanentDeleteAsync(long id)
        // {
        //     // var userId = _currentUserService.GetUserId();
        //     // var isAdmin = _currentUserService.IsAdmin();

        //     var entity = await _unitOfWork.Repository<UrgentCase>()
        //         .GetOneAsync(x => x.Id == id);

        //     if (entity == null)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Urgent case not found"
        //         };

        //     // Only admin can permanently delete
        //     // if (!isAdmin)
        //     //     return new ApiResponse<string>
        //     //     {
        //     //         Success = false,
        //     //         Message = "Only admins can permanently delete cases"
        //     //     };

        //     // Cannot permanently delete a Found case (FR‑16)
        //     if (entity.Status == CaseStatus.Found)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Cannot permanently delete a case that is marked as Founded"
        //         };

        //     _unitOfWork.Repository<UrgentCase>().Remove(entity);
        //     await _unitOfWork.SaveAsync();

        //     return new ApiResponse<string>
        //     {
        //         Success = true,
        //         Message = "Urgent case permanently deleted"
        //     };
        // }

        // // ------------------------- APPROVE (admin only) -------------------------
        // public async Task<ApiResponse<string>> ApproveAsync(long id)
        // {
        //     // if (!_currentUserService.IsAdmin())
        //     //     return new ApiResponse<string>
        //     //     {
        //     //         Success = false,
        //     //         Message = "Only admins can approve cases"
        //     //     };

        //     var entity = await _unitOfWork.Repository<UrgentCase>()
        //         .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted);

        //     if (entity == null)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Urgent case not found"
        //         };

        //     // Already active or found? Cannot approve.
        //     if (entity.Status == CaseStatus.Active || entity.Status == CaseStatus.Found)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = $"Case is already {entity.Status}"
        //         };

        //     // For urgent cases, approval might move from Pending to Active
        //     if (entity.Status == CaseStatus.Pending)
        //     {
        //         entity.Status = CaseStatus.Active;
        //         entity.PreviousStatus = CaseStatus.Pending;
        //         _unitOfWork.Repository<UrgentCase>().Update(entity);
        //         await _unitOfWork.SaveAsync();

        //         return new ApiResponse<string>
        //         {
        //             Success = true,
        //             Message = "Urgent case approved and activated"
        //         };
        //     }

        //     return new ApiResponse<string>
        //     {
        //         Success = false,
        //         Message = "Case cannot be approved in its current state"
        //     };
        // }

        // // ------------------------- REJECT (admin only) -------------------------
        // public async Task<ApiResponse<string>> RejectAsync(long id)
        // {
        //     // if (!_currentUserService.IsAdmin())
        //     //     return new ApiResponse<string>
        //     //     {
        //     //         Success = false,
        //     //         Message = "Only admins can reject cases"
        //     //     };

        //     var entity = await _unitOfWork.Repository<UrgentCase>()
        //         .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted && x.Status != CaseStatus.Found);

        //     if (entity == null)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Urgent case not found or cannot be rejected"
        //         };

        //     // Change status to Rejected
        //     entity.Status = CaseStatus.Rejected;
        //     entity.PreviousStatus = entity.Status;
        //     _unitOfWork.Repository<UrgentCase>().Update(entity);
        //     await _unitOfWork.SaveAsync();

        //     return new ApiResponse<string>
        //     {
        //         Success = true,
        //         Message = "Urgent case rejected"
        //     };
        // }

        // // ------------------------- MARK AS FOUNDED -------------------------
        // public async Task<ApiResponse<string>> MarkAsFoundedAsync(long id)
        // {
        //     // var userId = _currentUserService.GetUserId();
        //     // var isAdmin = _currentUserService.IsAdmin();

        //     var entity = await _unitOfWork.Repository<UrgentCase>()
        //         .GetOneAsync(x => x.Id == id && x.Status != CaseStatus.Deleted);

        //     if (entity == null)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Urgent case not found"
        //         };

        //     // Only owner or admin can mark as found
        //     // if (entity.UserId != userId && !isAdmin)
        //     //     return new ApiResponse<string>
        //     //     {
        //     //         Success = false,
        //     //         Message = "You are not authorized to mark this case as founded"
        //     //     };

        //     if (entity.Status == CaseStatus.Found)
        //         return new ApiResponse<string>
        //         {
        //             Success = false,
        //             Message = "Case is already marked as Founded"
        //         };

        //     entity.Status = CaseStatus.Found;
        //     entity.EndDate = DateTime.Now; // set the end date
        //     entity.PreviousStatus = entity.Status;
        //     _unitOfWork.Repository<UrgentCase>().Update(entity);
        //     await _unitOfWork.SaveAsync();

        //     return new ApiResponse<string>
        //     {
        //         Success = true,
        //         Message = "Case marked as Founded"
        //     };
        // }

        private static IQueryable<UrgentCase> ApplyFilter(IQueryable<UrgentCase> query, UrgentCaseFilterDto filter, bool includeDeleted = false, bool includeExpired = false)
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

            if (filter.Latitude.HasValue && filter.Longitude.HasValue && filter.RadiusInMeters.HasValue)
            {
                var userLocation = new Point(filter.Longitude.Value, filter.Latitude.Value)
                {
                    SRID = 4326
                };

                query = query.Where(x => x.Location != null && x.Location.Distance(userLocation) <= filter.RadiusInMeters.Value);
            }

            #endregion

            return query;
        }
        private static IQueryable<UrgentCase> ApplySorting(IQueryable<UrgentCase> query, UrgentCaseFilterDto filter)
        {
            IOrderedQueryable<UrgentCase>? orderedQuery = null;

            // Distance Sort
            if (filter.Latitude.HasValue && filter.Longitude.HasValue)
            {
                var userLocation = new Point(filter.Longitude.Value, filter.Latitude.Value)
                {
                    SRID = 4326
                };

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