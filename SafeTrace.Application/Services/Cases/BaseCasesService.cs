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

        /// <summary>
        /// Retrieves all active cases with filtering, sorting, and pagination.
        /// </summary>
        public virtual async Task<ApiResponse<PaginationResponseDto<TListDto>>> GetAllAsync(TFilterDto filter)
        {
            var query = _unitOfWork.Repository<TEntity>()
                .Query(tracked: false, includes: x => x.CaseFiles)
                .Where(x => x.Status == CaseStatus.Active);

            var response = await GetPagedResultAsync<TListDto>(query, filter);

            return ApiResponse<PaginationResponseDto<TListDto>>.Ok(response, "تم استرجاع الحالات بنجاح.");
        }
        
        /// <summary>
        /// Retrieves all cases for administrators with filtering, sorting and pagination.
        /// </summary>
        public virtual async Task<ApiResponse<PaginationResponseDto<TDetailDto>>> AdminGetAllAsync(TFilterDto filter)
        {
            var query = _unitOfWork.Repository<TEntity>().Query(
                    tracked: false, 
                    includes: [x => x.CaseFiles, x => x.User, x => x.AgeCategory, x => x.FoundPersonInfo]);

            var response = await GetPagedResultAsync<TDetailDto>(query, filter);

            return ApiResponse<PaginationResponseDto<TDetailDto>>.Ok(response, "تم استرجاع الحالات بنجاح.");
        }
        
        /// <summary>
        /// Retrieves the details of a specific active case by its ID.
        /// </summary>
        public virtual async Task<ApiResponse<TDetailDto>> GetByIdAsync(long id)
        {
            var dto = await GetByIdInternalAsync<TDetailDto>(
                id,
                activeOnly: true,
                includes: [x => x.CaseFiles, x => x.User, x => x.AgeCategory]);

            await AfterGetByIdAsync(dto, id,false);

            return ApiResponse<TDetailDto>.Ok(dto, "تم استرجاع بيانات الحالة بنجاح.");
        }

        /// <summary>
        /// Retrieves the details of a specific case by its ID for administrators.
        /// </summary>
        public virtual async Task<ApiResponse<TDetailDto>> AdminGetByIdAsync(long id)
        {
            var dto = await GetByIdInternalAsync<TDetailDto>(
                id,
                activeOnly: false,
                includes: [x => x.CaseFiles, x => x.User, x => x.AgeCategory, x => x.FoundPersonInfo]);
            await AfterGetByIdAsync(dto, id, true);
            return ApiResponse<TDetailDto>.Ok(dto, "تم استرجاع بيانات الحالة بنجاح.");
        }
        
        /// <summary>
        /// Approves a pending case and changes its status to Active.
        /// </summary>
        public virtual async Task<ApiResponse<string>> ApproveAsync(long caseId)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(caseId);

            if (entity.Status == CaseStatus.Active)
                throw new BadRequestException("تمت الموافقة على الحالة بالفعل.");

            if (entity.Status == CaseStatus.Rejected)
                throw new BadRequestException("لا يمكن الموافقة على حالة مرفوضة.");

            if (entity.Status != CaseStatus.Pending)
                throw new BadRequestException("يمكن الموافقة على الحالات قيد المراجعة فقط.");

            await ExecuteInTransactionAsync(
                async () =>
                {
                    entity.Status = CaseStatus.Active;
                    entity.PreviousStatus = null;
                    entity.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<TEntity>().Update(entity);

                    return true;
                });

            _logger.LogInformation("Case {CaseId} approved.", entity.Id);

            return ApiResponse<string>.Ok(message: "تمت الموافقة على الحالة بنجاح.");
        }
        
        /// <summary>
        /// Rejects a pending case or restores its previous status if available.
        /// </summary>
        public virtual async Task<ApiResponse<string>> RejectAsync(long caseId)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(caseId);

            if (entity.Status == CaseStatus.Rejected)
                throw new BadRequestException("تم رفض الحالة بالفعل.");

            if (entity.Status == CaseStatus.Active)
                throw new BadRequestException("لا يمكن رفض حالة تمت الموافقة عليها.");

            if (entity.Status != CaseStatus.Pending)
                throw new BadRequestException("يمكن رفض الحالات قيد المراجعة فقط.");

            await ExecuteInTransactionAsync(
                async () =>
                {
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

                    return true;
                });

            _logger.LogInformation(
                "Case {CaseId} rejected (Pending -> {Status}).",
                entity.Id,
                entity.Status);
            
            return ApiResponse<string>.Ok(message: "تم رفض الحالة بنجاح.");
        }
        
        /// <summary>
        /// Soft deletes a case while preserving its data for future recovery.
        /// </summary>
        public virtual async Task<ApiResponse<string>> SoftDeleteAsync(long caseId, string userId, bool checkOwnership = true)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(
                caseId,
                userId,
                checkOwnership: checkOwnership,
                includes: x => x.CaseFiles);

            if (entity.Status == CaseStatus.Found)
            {
                throw new BadRequestException("لا يمكن حذف حالة تم العثور عليها.");
            }

            var faceIds = entity.CaseFiles
                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                .Select(p => p.FaceId!)
                .ToList();

            await ExecuteInTransactionAsync(
                async () =>
                {
                    entity.PreviousStatus = entity.Status;
                    entity.Status = CaseStatus.Deleted;
                    entity.DeletedAt = DateTime.UtcNow;
                    entity.DeletedByUserId = userId;
                    entity.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<TEntity>().Update(entity);

                    return true;
                });

            await _caseHelper.DeleteFacesAsync(faceIds, entity.Id);

            _logger.LogInformation(
                "Case {CaseId} soft-deleted by user {UserId}.",
                entity.Id,
                userId);
            
            return ApiResponse<string>.Ok(message: "تم حذف الحالة بنجاح.");
        }
        
        /// <summary>
        /// Marks a case as found and stores the found person information.
        /// </summary>
        public virtual async Task<ApiResponse<string>> MarkAsFoundAsync(long caseId, string userId, FoundPersonInfoRequestDto foundPersonInfo, bool checkOwnership = true)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(caseId, userId, checkOwnership);

            if (entity.Status == CaseStatus.Found)
                throw new BadRequestException("تم تسجيل هذه الحالة كمُعثر عليها بالفعل.");

            if (entity.Status == CaseStatus.Expired)
                throw new BadRequestException("لا يمكن تسجيل حالة منتهية كمُعثر عليها.");

            await ExecuteInTransactionAsync(
                async () =>
                {
                    entity.PreviousStatus = entity.Status;
                    entity.Status = CaseStatus.Found;
                    entity.UpdatedAt = DateTime.UtcNow;

                    await OnMarkedAsFoundAsync(entity);

                    var foundPersonInfoEntity = _mapper.Map<FoundPersonInfo>(foundPersonInfo);
                    foundPersonInfoEntity.CaseId = entity.Id;
                    foundPersonInfoEntity.FoundedUserId = userId;

                    await _unitOfWork.Repository<FoundPersonInfo>()
                        .CreateAsync(foundPersonInfoEntity);

                    _unitOfWork.Repository<TEntity>().Update(entity);

                    return true;
                });

            _logger.LogInformation(
                "Case {CaseId} marked as Found by user {UserId}.",
                entity.Id,
                userId);
            
            return ApiResponse<string>.Ok(message: "تم تسجيل الحالة كمُعثر عليها بنجاح.");
        }

        /// <summary>
        /// Permanently deletes a case and removes all associated files and face records.
        /// </summary> 
        public virtual async Task<ApiResponse<string>> PermanentDeleteAsync(long caseId)
        {
            var entity = await _unitOfWork.Repository<TEntity>()
                .GetOneAsync(c => c.Id == caseId, tracked: true, includes: c => c.CaseFiles);

            if (entity == null)
                throw new NotFoundException("الحالة غير موجودة.");

            var filesToDelete = entity.CaseFiles
                .Select(p => p.ImagePath)
                .ToList();

            var faceIds = entity.CaseFiles
                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                .Select(p => p.FaceId!)
                .ToList();

            await ExecuteInTransactionAsync(
                async () =>
                {
                    _unitOfWork.Repository<TEntity>().Remove(entity);

                    return true;
                });

            _caseHelper.CleanupPhysicalFiles(filesToDelete);

            DeleteAdditionalFiles(entity);

            await _caseHelper.DeleteFacesAsync(faceIds, entity.Id);

            _logger.LogWarning(
                "Case {CaseId} permanently deleted.",
                entity.Id);
            
            return ApiResponse<string>.Ok(message: "تم حذف الحالة نهائيًا بنجاح.");
        }
                
        /// <summary>
        /// Executes the specified operation inside a transaction,
        /// committing on success and rolling back on failure.
        /// </summary>
        protected async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action, Func<Exception, Task>? onFailureAsync = null)
        {
            await _unitOfWork.BeginTransactionAsync();
 
            try
            {
                var result = await action();
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
 
                if (onFailureAsync != null)
                    await onFailureAsync(ex);
 
                throw;
            }
        }
        
        // EXTENSION HOOKS (Template Method) — only where a real difference exists

        /// <summary>
        /// Deletes any feature-specific physical files beyond the shared Photos collection. No-op by default.
        /// </summary>
        protected virtual void DeleteAdditionalFiles(TEntity entity){}

        /// <summary>
        /// Extra state changes when a case is marked as Found (e.g. Urgent sets EndDate). No-op by default.
        /// </summary>
        protected virtual Task OnMarkedAsFoundAsync(TEntity entity) => Task.CompletedTask;
        
        /// <summary>
        /// Builds the base query used by <see cref="GetAllAsync"/> before shared filtering, sorting,
        /// and pagination (see <see cref="GetPagedResultAsync{TDto}"/>) are applied.
        /// Default: active cases with their CaseFiles included.
        /// Derived services can override this to change includes or pre-shape the query
        /// (e.g. duplicate-group deduplication) without touching the shared filter/sort/pagination/mapping pipeline.
        /// </summary>
        protected virtual IQueryable<TEntity> BuildGetAllQuery()
        {
            return _unitOfWork.Repository<TEntity>()
                .Query(tracked: false, includes: x => x.CaseFiles)
                .Where(x => x.Status == CaseStatus.Active);
        }

        /// <summary>
        /// Hook invoked by <see cref="GetByIdAsync"/> after the entity has been mapped to <typeparamref name="TDetailDto"/>,
        /// allowing derived services to enrich the DTO with feature-specific data
        /// (e.g. populating related/duplicate cases). No-op by default.
        /// </summary>
        protected virtual Task AfterGetByIdAsync(TDetailDto dto, long id, bool isAdmin) => Task.CompletedTask;

        /// <summary>
        /// Allows derived services to apply additional filtering. Default: no extra filters. 
        /// </summary>
        protected virtual IQueryable<TEntity> ApplyCustomFilter(IQueryable<TEntity> query, TFilterDto filter)=> query;

        /// <summary>
        /// Allows derived services to apply custom sorting. Default: no extra sorting.
        /// </summary>
        protected virtual IQueryable<TEntity> ApplyCustomSorting(IQueryable<TEntity> query, TFilterDto filter) => query; 
        
        // FILTER / SORT / PAGINATION
        private IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, TFilterDto filter)
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
                var keyword = filter.FullName.Trim();

                query = query.Where(x =>
                    x.FName.Contains(keyword) ||
                    x.SName.Contains(keyword) ||
                    x.TName.Contains(keyword) ||
                    x.LName.Contains(keyword));
            }

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);
            
            // Feature-specific filters
            query = ApplyCustomFilter(query, filter);

            return query;
        }
        private IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, TFilterDto filter)
        {
            IOrderedQueryable<TEntity>? orderedQuery = null;

            if (!string.IsNullOrWhiteSpace(filter.FullName))
            {
                var keyword = filter.FullName.Trim();

                orderedQuery = query.OrderBy(x =>
                    x.FName.Contains(keyword) ? 0 :
                    x.SName.Contains(keyword) ? 1 :
                    x.TName.Contains(keyword) ? 2 :
                    x.LName.Contains(keyword) ? 3 : 4);
            }

            query = ApplyCustomSorting(query, filter);

            orderedQuery ??= query as IOrderedQueryable<TEntity>;

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
        private static IQueryable<TEntity> ApplyPagination(IQueryable<TEntity> query, TFilterDto filter)
        {
            filter.Page = Math.Max(filter.Page, 1);

            return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
        }
        
        // Applies filtering, sorting, and pagination, then maps the result to the requested DTO.
        protected async Task<PaginationResponseDto<TDto>> GetPagedResultAsync<TDto>(IQueryable<TEntity> query, TFilterDto filter)
        {
            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, filter);
            query = ApplyPagination(query, filter);

            var items = await query.ToListAsync();

            return new PaginationResponseDto<TDto>
            {
                Items = _mapper.Map<List<TDto>>(items),
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        // Retrieves a case by ID, validates its status if required, and maps it to the requested DTO.
        protected  async Task<TDto> GetByIdInternalAsync<TDto>(long id, bool activeOnly, params Expression<Func<TEntity, object>>[] includes)
        {
            var entity = await _caseHelper.GetValidCaseAsync<TEntity>(
                id,
                tracked: false,
                includes: includes);

            if (activeOnly && entity.Status != CaseStatus.Active)
                throw new NotFoundException("الحالة غير موجودة.");

            return _mapper.Map<TDto>(entity);
        }
    
    }
}