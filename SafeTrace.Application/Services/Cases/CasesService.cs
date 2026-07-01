using NetTopologySuite.Geometries;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.SharedCases;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;


namespace SafeTrace.Application.Services.Cases
{
    public class CasesService : ICasesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICaseHelperService _caseHelper;
        private readonly ILogger<CasesService> _logger;

        public CasesService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            ICaseHelperService caseHelper, 
            ILogger<CasesService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _caseHelper = caseHelper;
            _logger = logger;
        }

        // QUERIES
        public async Task<ApiResponse<PaginationResponseDto<CaseListItemDto>>> GetCasesAsync(CasesFilterDto filter, Point? userLocation = null, string? userId = null)
        {
            var query = _unitOfWork.Repository<Case>().Query(tracked: false, includes: x => x.Photos);

            // Apply standard active case filter
            query = query.Where(x => x.Status == CaseStatus.Active);

            query = ApplyFilter(query, filter, userLocation);
            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter, userLocation);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();
            var response = new PaginationResponseDto<CaseListItemDto>
            {
                Items = _mapper.Map<List<CaseListItemDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<CaseListItemDto>>.Ok(response, "Cases retrieved successfully.");
        }

        public async Task<ApiResponse<CaseDetailDto>> GetCaseByIdAsync(long id)
        {
            var entity = await _unitOfWork.Repository<Case>()
                .GetOneAsync(
                    x => x.Id == id && x.Status != CaseStatus.Deleted,
                    tracked: false,
                    includes: [x => x.Photos, x => x.User, x => x.AgeCategory]
                );

            if (entity == null)
            {
                throw new NotFoundException($"Case {id} not found.");
            }

            var dto = _mapper.Map<CaseDetailDto>(entity);
            return ApiResponse<CaseDetailDto>.Ok(dto, "Case retrieved successfully.");
        }
        
        public async Task<ApiResponse<PaginationResponseDto<CaseListItemDto>>> GetMyCasesAsync(string userId, CasesFilterDto filter)
        {
            var query = _unitOfWork.Repository<Case>()
                .Query(tracked: false, includes: x => x.Photos)
                .Where(x => x.UserId == userId && x.Status != CaseStatus.Deleted);

            query = ApplyFilter(query, filter, null); // location proximity doesn't usually apply to "my cases"

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter, null);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();
            var response = new PaginationResponseDto<CaseListItemDto>
            {
                Items = _mapper.Map<List<CaseListItemDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<CaseListItemDto>>.Ok(response, "My cases retrieved successfully.");
        }
        
        public async Task<ApiResponse<PaginationResponseDto<CaseDetailDto>>> AdminGetCasesAsync(CasesFilterDto filter)
        {
            // Admins can see all statuses, so we don't pre-filter by Active or Non-deleted
            var query = _unitOfWork.Repository<Case>().Query(
                tracked: false,
                includes: [x => x.Photos, x => x.User, x => x.AgeCategory, x => x.FoundPersonInfo]
            );

            query = ApplyFilter(query, filter, null); // Location usually doesn't matter for admin view unless specified

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter, null);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();
            var response = new PaginationResponseDto<CaseDetailDto>
            {
                Items = _mapper.Map<List<CaseDetailDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<CaseDetailDto>>.Ok(response, "Admin cases retrieved successfully.");
        }

        private static IQueryable<Case> ApplyFilter(IQueryable<Case> query, CasesFilterDto filter, Point? userLocation)
        {
            if (filter.CaseType.HasValue)
                query = query.Where(x => x.CaseType == filter.CaseType.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.Gender.HasValue)
                query = query.Where(x => x.Gender == filter.Gender.Value);

            if (filter.MinAge.HasValue)
                query = query.Where(x => x.Age >= filter.MinAge.Value);

            if (filter.MaxAge.HasValue)
                query = query.Where(x => x.Age <= filter.MaxAge.Value);

            if (!string.IsNullOrWhiteSpace(filter.Government))
                query = query.Where(x => x.Government.Contains(filter.Government));

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(x => x.City.Contains(filter.City));

            if (!string.IsNullOrWhiteSpace(filter.FullName))
            {
                var name = filter.FullName.Trim().ToLower();
                query = query.Where(x =>
                    ((x.FName ?? "") + " " + (x.SName ?? "") + " " + (x.TName ?? "") + " " + (x.LName ?? "")).Contains(name, StringComparison.CurrentCultureIgnoreCase)
                );
            }

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);

            if (userLocation != null)
            {
                // Only Urgent cases typically use Location for distance filtering, but if it exists on Base or UrgentCase only:
                // Since Location is only on UrgentCase, this will require a cast or an EF.Property check in EF Core.
                // Assuming EF Core 8/9 where we can use OFTYPE or just cast
                query = query.Where(x =>
                    x is UrgentCase && ((UrgentCase)x).Location.Distance(userLocation) <= filter.RadiusInMeters
                    || !(x is UrgentCase) // For non-urgent cases, bypass distance filter if location is only on urgent case
                );
            }

            return query;
        }
        
        private static IQueryable<Case> ApplySorting(IQueryable<Case> query, CasesFilterDto filter, Point? userLocation)
        {
            IOrderedQueryable<Case>? orderedQuery = null;

            if (userLocation != null)
            {
                // Ordering by distance (only works if they are UrgentCases or have location)
                orderedQuery = query.OrderBy(x => x is UrgentCase ? ((UrgentCase)x).Location.Distance(userLocation) : double.MaxValue);
            }

            if (filter.AgeSort.HasValue)
            {
                var asc = filter.AgeSort == AgeSort.Asc;
                orderedQuery = orderedQuery == null
                    ? (asc ? query.OrderBy(x => x.Age) : query.OrderByDescending(x => x.Age))
                    : (asc ? orderedQuery.ThenBy(x => x.Age) : orderedQuery.ThenByDescending(x => x.Age));
            }

            if (filter.DateSort.HasValue)
            {
                var newest = filter.DateSort == DateSort.Newest;
                orderedQuery = orderedQuery == null
                    ? (newest ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt))
                    : (newest ? orderedQuery.ThenByDescending(x => x.CreatedAt) : orderedQuery.ThenBy(x => x.CreatedAt));
            }

            return orderedQuery ?? query.OrderByDescending(x => x.CreatedAt);
        }
        
        private static IQueryable<Case> ApplyPagination(IQueryable<Case> query, CasesFilterDto filter)
        {
            filter.Page = filter.Page <= 0 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize <= 0 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);
            return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
        }

        // SHARED COMMANDS
        public async Task ApproveAsync(long caseId)
        {
            var entity = await _caseHelper.GetValidCaseAsync<Case>(caseId);

            if (entity.Status == CaseStatus.Active)
                throw new BadRequestException("Case is already approved.");

            if (entity.Status == CaseStatus.Rejected)
                throw new BadRequestException("Cannot approve a rejected case.");

            if (entity.Status != CaseStatus.Pending)
                throw new BadRequestException("Only pending cases can be approved.");

            entity.Status = CaseStatus.Active;
            entity.PreviousStatus = null;
            entity.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Case>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} approved.", entity.Id);
        }

        public async Task RejectAsync(long caseId)
        {
            var entity = await _caseHelper.GetValidCaseAsync<Case>(caseId);

            if (entity.Status == CaseStatus.Rejected)
                throw new BadRequestException("Case is already rejected.");

            if (entity.Status == CaseStatus.Active)
                throw new BadRequestException("Cannot reject an already active/approved case.");

            if (entity.Status != CaseStatus.Pending)
                throw new BadRequestException("Only pending cases can be rejected.");

            if (entity.PreviousStatus.HasValue)
            {
                entity.Status = entity.PreviousStatus.Value;
                entity.PreviousStatus = null;
            }
            else
            {
                entity.Status = CaseStatus.Rejected;
            }

            entity.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Case>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} rejected (Pending -> {Status}).", entity.Id, entity.Status);
        }

        public async Task SoftDeleteAsync(long caseId, string userId, bool isAdmin = false, bool checkOwnership = true)
        {
            var entity = await _caseHelper.GetValidCaseAsync<Case>(caseId, userId, isAdmin, checkOwnership: checkOwnership);

            if (entity.Status == CaseStatus.Found)
            {
                _logger.LogWarning("Attempt to delete Case {CaseId} that is already marked as Found.", entity.Id);

                throw new BadRequestException("Cannot delete a case that is already marked as Found.");
            }

            entity.PreviousStatus = entity.Status;
            entity.Status = CaseStatus.Deleted;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = userId;
            entity.UpdatedAt = DateTime.UtcNow;

            // Delete face records
            var faceIds = entity.Photos
                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                .Select(p => p.FaceId!)
                .ToList();
            
            await _caseHelper.DeleteFacesAsync(faceIds, entity.Id);

            _unitOfWork.Repository<Case>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} soft-deleted by user {UserId}.", entity.Id, userId);
        }

        public async Task MarkAsFoundAsync(long caseId, string userId, FoundPersonInfo? foundPersonInfo = null, bool isAdmin = false, bool checkOwnership = true)
        {
            var entity = await _caseHelper.GetValidCaseAsync<Case>(caseId, userId, isAdmin, checkOwnership: checkOwnership);

            if (entity.Status == CaseStatus.Found)
                throw new BadRequestException("Case is already marked as Found.");

            if (entity.Status == CaseStatus.Expired)
                throw new BadRequestException("Cannot mark an expired case as Found.");

            entity.PreviousStatus = entity.Status;
            entity.Status = CaseStatus.Found;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entity is UrgentCase urgentCase)
            {
                urgentCase.EndDate = DateTime.UtcNow;
            }

            if (foundPersonInfo != null)
            {
                foundPersonInfo.CaseId = entity.Id;
                await _unitOfWork.Repository<FoundPersonInfo>().CreateAsync(foundPersonInfo);
            }

            _unitOfWork.Repository<Case>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} marked as Found by user {UserId}.", entity.Id, userId);
        }

        public async Task PermanentDeleteAsync(long caseId)
        {
            var entity = await _unitOfWork.Repository<Case>()
                .GetOneAsync(
                    c => c.Id == caseId,
                    tracked: true,
                    includes: c => c.Photos);

            if (entity == null)
            {
                _logger.LogWarning("Permanent delete failed - Case {CaseId} not found or not soft-deleted.", caseId);

                throw new NotFoundException($"Case {caseId} was not found or has not been soft-deleted yet. Permanent delete requires soft delete first.");
            }

            // Delete photos
            var filesToDelete = entity.Photos.Select(p => p.ImagePath).ToList();

            await _caseHelper.CleanupPhysicalFilesAsync(
                filesToDelete, 
                entity is LongTermMissingCase longTermCase ? longTermCase.PoliceReportImage : null);

            // Delete face records
            var faceIds = entity.Photos
                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                .Select(p => p.FaceId!)
                .ToList();

            await _caseHelper.DeleteFacesAsync(faceIds, entity.Id);

            _unitOfWork.Repository<Case>().Remove(entity);

            await _unitOfWork.SaveAsync();

            _logger.LogWarning("Case {CaseId} permanently deleted.", entity.Id);
        }
    }
}