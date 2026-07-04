using System.Linq.Expressions;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.Application.Services.Cases
{
    public abstract class BaseCasesService<TEntity, TListDto, TDetailDto, TFilterDto> : IBaseCasesService<TListDto, TDetailDto, TFilterDto>
        where TEntity : Case
        where TFilterDto : CasesFilterBaseDto
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;
        protected readonly ICaseHelperService _caseHelper;
        protected readonly ILogger _logger;

        protected BaseCasesService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
            ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _caseHelper = caseHelper;
            _logger = logger;
        }

        // QUERIES
        public virtual async Task<ApiResponse<PaginationResponseDto<TListDto>>> GetAllAsync(TFilterDto filter)
        {
            var query = _unitOfWork.Repository<TEntity>().Query(tracked: false, includes: x => x.Photos);

            query = query.Where(x => x.Status == CaseStatus.Active);
            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            var response = new PaginationResponseDto<TListDto>
            {
                Items = _mapper.Map<List<TListDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<TListDto>>.Ok(response, "Cases retrieved successfully.");
        }

        public virtual async Task<ApiResponse<TDetailDto>> GetByIdAsync(long id)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(id, tracked: false, includes: DetailIncludes);

            var dto = _mapper.Map<TDetailDto>(entity);
            return ApiResponse<TDetailDto>.Ok(dto, "Case retrieved successfully.");
        }

        public virtual async Task<ApiResponse<PaginationResponseDto<TListDto>>> GetMyCasesAsync(string userId, TFilterDto filter)
        {
            var query = _unitOfWork.Repository<TEntity>()
                .Query(tracked: false, includes: x => x.Photos)
                .Where(x => x.UserId == userId && x.Status != CaseStatus.Deleted);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            var response = new PaginationResponseDto<TListDto>
            {
                Items = _mapper.Map<List<TListDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<TListDto>>.Ok(response, "My cases retrieved successfully.");
        }

        public virtual async Task<ApiResponse<PaginationResponseDto<TDetailDto>>> AdminGetAllAsync(TFilterDto filter)
        {
            // Admins can see all statuses, so no Active/non-deleted pre-filter here.
            var query = _unitOfWork.Repository<TEntity>().Query(tracked: false, includes: AdminDetailIncludes);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            var response = new PaginationResponseDto<TDetailDto>
            {
                Items = _mapper.Map<List<TDetailDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<TDetailDto>>.Ok(response, "Admin cases retrieved successfully.");
        }

        // SHARED COMMANDS
        public virtual async Task ApproveAsync(long caseId)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(caseId);

            if (entity.Status == CaseStatus.Active)
                throw new BadRequestException("Case is already approved.");

            if (entity.Status == CaseStatus.Rejected)
                throw new BadRequestException("Cannot approve a rejected case.");

            if (entity.Status != CaseStatus.Pending)
                throw new BadRequestException("Only pending cases can be approved.");

            entity.Status = CaseStatus.Active;
            entity.PreviousStatus = null;
            entity.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<TEntity>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} approved.", entity.Id);
        }

        public virtual async Task RejectAsync(long caseId)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(caseId);

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

            _unitOfWork.Repository<TEntity>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} rejected (Pending -> {Status}).", entity.Id, entity.Status);
        }

        public virtual async Task SoftDeleteAsync(long caseId, string userId, bool isAdmin = false, bool checkOwnership = true)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(caseId, userId, isAdmin, checkOwnership: checkOwnership);

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

            var faceIds = entity.Photos
                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                .Select(p => p.FaceId!)
                .ToList();

            await _caseHelper.DeleteFacesAsync(faceIds, entity.Id);

            _unitOfWork.Repository<TEntity>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} soft-deleted by user {UserId}.", entity.Id, userId);
        }

        public virtual async Task MarkAsFoundAsync(long caseId, string userId, FoundPersonInfoRequestDto foundPersonInfo, bool isAdmin = false, bool checkOwnership = true)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(caseId, userId, isAdmin, checkOwnership);

            if (entity.Status == CaseStatus.Found)
                throw new BadRequestException("Case is already marked as Found.");

            if (entity.Status == CaseStatus.Expired)
                throw new BadRequestException("Cannot mark an expired case as Found.");

            entity.PreviousStatus = entity.Status;
            entity.Status = CaseStatus.Found;
            entity.UpdatedAt = DateTime.UtcNow;

            await OnMarkedAsFoundAsync(entity);

            var foundPersonInfoEntity = _mapper.Map<FoundPersonInfo>(foundPersonInfo);

            foundPersonInfoEntity.CaseId = entity.Id;
            foundPersonInfoEntity.FoundedUserId = userId;

            await _unitOfWork.Repository<FoundPersonInfo>().CreateAsync(foundPersonInfoEntity);

            _unitOfWork.Repository<TEntity>().Update(entity);

            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Case {CaseId} marked as Found by user {UserId}.", entity.Id, userId);
        }

        public virtual async Task PermanentDeleteAsync(long caseId)
        {
            var entity = await _unitOfWork.Repository<TEntity>()
                .GetOneAsync(
                    c => c.Id == caseId,
                    tracked: true,
                    includes: c => c.Photos);

            if (entity == null)
            {
                _logger.LogWarning("Permanent delete failed - Case {CaseId} not found or not soft-deleted.", caseId);
                throw new NotFoundException($"Case {caseId} was not found or has not been soft-deleted yet. Permanent delete requires soft delete first.");
            }

            // 1. Shared photo files
            var filesToDelete = entity.Photos.Select(p => p.ImagePath).ToList();
            await _caseHelper.CleanupPhysicalFilesAsync(filesToDelete);

            // 2. Feature-specific files (e.g. LongTermMissingCase.PoliceReportImage) — hook, not a type check.
            await DeleteAdditionalFilesAsync(entity);

            // 3. Shared face records
            var faceIds = entity.Photos
                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                .Select(p => p.FaceId!)
                .ToList();

            await _caseHelper.DeleteFacesAsync(faceIds, entity.Id);

            // 4/5. Remove + save
            _unitOfWork.Repository<TEntity>().Remove(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogWarning("Case {CaseId} permanently deleted.", entity.Id);
        }

        // EXTENSION HOOKS (Template Method) — only where a real difference exists

        /// <summary>Deletes any feature-specific physical files beyond the shared Photos collection. No-op by default.</summary>
        protected virtual Task DeleteAdditionalFilesAsync(TEntity entity) => Task.CompletedTask;

        /// <summary>Extra state changes when a case is marked as Found (e.g. Urgent sets EndDate). No-op by default.</summary>
        protected virtual Task OnMarkedAsFoundAsync(TEntity entity) => Task.CompletedTask;

        /// <summary>Includes used for GetByIdAsync. Override to add type-specific navigation properties.</summary>
        protected virtual Expression<Func<TEntity, object>>[] DetailIncludes => [x => x.Photos, x => x.User, x => x.AgeCategory];

        /// <summary>Includes used for AdminGetAllAsync. Override to add type-specific navigation properties.</summary>
        protected virtual Expression<Func<TEntity, object>>[] AdminDetailIncludes => [x => x.Photos, x => x.User, x => x.AgeCategory, x => x.FoundPersonInfo];
        
        /// <summary>Allows derived services to apply additional filtering. Default: no extra filters. </summary>
        protected virtual IQueryable<TEntity> ApplyCustomFilter(IQueryable<TEntity> query, TFilterDto filter)=> query;

        /// <summary>Allows derived services to apply custom sorting. Default: no extra sorting.</summary>
        protected virtual IQueryable<TEntity> ApplyCustomSorting(IQueryable<TEntity> query, TFilterDto filter) => query; 
        

        // FILTER / SORT / PAGINATION
        protected virtual IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, TFilterDto filter)
        {
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
            
            // Feature-specific filters
            query = ApplyCustomFilter(query, filter);

            return query;
        }
        protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, TFilterDto filter)
        {
            query = ApplyCustomSorting(query, filter);

            IOrderedQueryable<TEntity>? orderedQuery = query as IOrderedQueryable<TEntity>;

            if (filter.AgeSort.HasValue)
            {
                var asc = filter.AgeSort == AgeSort.Asc;
                orderedQuery = asc ? query.OrderBy(x => x.Age) : query.OrderByDescending(x => x.Age);
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
        protected virtual IQueryable<TEntity> ApplyPagination(IQueryable<TEntity> query, TFilterDto filter)
        {
            filter.Page = filter.Page <= 0 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize <= 0 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);
            return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
        }
    }
}