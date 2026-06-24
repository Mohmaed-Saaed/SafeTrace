// using AutoMapper;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using NetTopologySuite.Geometries;
// using Serilog;
// using System.Security.Claims;

// namespace SafeTrace.Application.Services
// {
//     public class UrgentCaseService : IUrgentCaseService
//     {
//         private readonly IUnitOfWork _uow;
//         private readonly INotificationService _notificationService;
//         private readonly UserManager<ApplicationUser> _userManager;
//         private readonly IHttpContextAccessor _httpContextAccessor;
//         private readonly IMapper _mapper;
//         private readonly IFileStorageService _fileStorage;

//         private const int ExpirationHours = 48;
//         private const int RateLimitDays = 14;
//         private const double NotificationRadiusMeters = 10_000;

//         public UrgentCaseService(
//             IUnitOfWork uow,
//             INotificationService notificationService,
//             UserManager<ApplicationUser> userManager,
//             IHttpContextAccessor httpContextAccessor,
//             IMapper mapper,
//             IFileStorageService fileStorage)
//         {
//             _uow = uow;
//             _notificationService = notificationService;
//             _userManager = userManager;
//             _httpContextAccessor = httpContextAccessor;
//             _mapper = mapper;
//             _fileStorage = fileStorage;
//         }

//         // ── Helpers ──────────────────────────────────────────────────────────────

//         private string? GetCurrentUserId()
//             => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

//         private static string GenerateCaseCode()
//             => $"UC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

//         /// <summary>
//         /// Builds a base AsNoTracking query with all needed includes.
//         /// </summary>
//         private IQueryable<UrgentCase> BaseQuery()
//             => _uow.Repository<UrgentCase>()
//                    .Query(tracked: false,
//                           includes: new System.Linq.Expressions.Expression<Func<UrgentCase, object>>[]
//                           {
//                               c => c.User,
//                               c => c.Photos,
//                               c => c.AgeCategory
//                           });

//         /// <summary>
//         /// Applies all optional filter criteria to a query.
//         /// </summary>
//         private static IQueryable<UrgentCase> ApplyFilters(
//             IQueryable<UrgentCase> query,
//             UrgentCaseFilterDto filter)
//         {
//             if (filter.Gender.HasValue)
//                 query = query.Where(c => c.Gender == filter.Gender.Value);

//             if (!string.IsNullOrWhiteSpace(filter.Government))
//                 query = query.Where(c => c.Government.ToLower().Contains(filter.Government.ToLower()));

//             if (!string.IsNullOrWhiteSpace(filter.City))
//                 query = query.Where(c => c.City.ToLower().Contains(filter.City.ToLower()));

//             if (filter.MinAge.HasValue)
//                 query = query.Where(c => c.Age >= filter.MinAge.Value);

//             if (filter.MaxAge.HasValue)
//                 query = query.Where(c => c.Age <= filter.MaxAge.Value);

//             if (filter.FromDate.HasValue)
//                 query = query.Where(c => c.CreatedAt >= filter.FromDate.Value);

//             if (filter.ToDate.HasValue)
//                 query = query.Where(c => c.CreatedAt <= filter.ToDate.Value);

//             return query;
//         }

//         /// <summary>
//         /// Applies sorting; defaults to newest-first.
//         /// </summary>
//         private static IQueryable<UrgentCase> ApplySorting(
//             IQueryable<UrgentCase> query,
//             UrgentCaseFilterDto filter)
//         {
//             if (filter.AgeSort.HasValue)
//                 return filter.AgeSort == AgeSort.Ascending
//                     ? query.OrderBy(c => c.Age)
//                     : query.OrderByDescending(c => c.Age);

//             if (filter.DateSort.HasValue)
//                 return filter.DateSort == DateSort.Ascending
//                     ? query.OrderBy(c => c.CreatedAt)
//                     : query.OrderByDescending(c => c.CreatedAt);

//             return query.OrderByDescending(c => c.CreatedAt);
//         }

//         /// <summary>
//         /// Executes count + paginated fetch and maps to DTOs.
//         /// </summary>
//         private async Task<PaginationResponseDto<UrgentCaseListItemDto>> PaginateAsync(
//             IQueryable<UrgentCase> query,
//             UrgentCaseFilterDto filter)
//         {
//             int page     = filter.Page     < 1 ? 1  : filter.Page;
//             int pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

//             int totalCount = await query.CountAsync();

//             var items = await query
//                 .Skip((page - 1) * pageSize)
//                 .Take(pageSize)
//                 .ToListAsync();

//             return new PaginationResponseDto<UrgentCaseListItemDto>
//             {
//                 Items      = _mapper.Map<List<UrgentCaseListItemDto>>(items),
//                 PageNumber = page,
//                 PageSize   = pageSize,
//                 TotalCount = totalCount
//             };
//         }

//         // ── Queries ──────────────────────────────────────────────────────────────

//         public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetAllAsync(
//             UrgentCaseFilterDto filter)
//         {
//             var query = BaseQuery().Where(c => c.DeletedAt == null);
//             query = ApplyFilters(query, filter);
//             query = ApplySorting(query, filter);
//             return ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>.Ok(
//                 await PaginateAsync(query, filter));
//         }

//         public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetFoundedAsync(
//             UrgentCaseFilterDto filter)
//         {
//             var query = BaseQuery().Where(c => c.Status == CaseStatus.Founded);
//             query = ApplyFilters(query, filter);
//             query = ApplySorting(query, filter);
//             return ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>.Ok(
//                 await PaginateAsync(query, filter));
//         }

//         public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetDeletedAsync(
//             UrgentCaseFilterDto filter)
//         {
//             var query = BaseQuery().Where(c => c.DeletedAt != null);
//             query = ApplyFilters(query, filter);
//             query = ApplySorting(query, filter);
//             return ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>.Ok(
//                 await PaginateAsync(query, filter));
//         }

//         public async Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetPendingAsync(
//             UrgentCaseFilterDto filter)
//         {
//             var query = BaseQuery().Where(c => c.Status == CaseStatus.Pending && c.DeletedAt == null);
//             query = ApplyFilters(query, filter);
//             query = ApplySorting(query, filter);
//             return ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>.Ok(
//                 await PaginateAsync(query, filter));
//         }

//         public async Task<ApiResponse<UrgentCaseDetailDto>> GetByIdAsync(long id)
//         {
//             var urgentCase = await BaseQuery()
//                 .FirstOrDefaultAsync(c => c.Id == id);

//             if (urgentCase is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Urgent case not found.");

//             return ApiResponse<UrgentCaseDetailDto>.Ok(_mapper.Map<UrgentCaseDetailDto>(urgentCase));
//         }

//         public async Task<ApiResponse<List<UrgentCaseListItemDto>>> GetMyCasesAsync()
//         {
//             var userId = GetCurrentUserId();
//             if (userId is null)
//                 return ApiResponse<List<UrgentCaseListItemDto>>.Fail("Unauthorized.");

//             var cases = await BaseQuery()
//                 .Where(c => c.UserId == userId)
//                 .OrderByDescending(c => c.CreatedAt)
//                 .ToListAsync();

//             return ApiResponse<List<UrgentCaseListItemDto>>.Ok(
//                 _mapper.Map<List<UrgentCaseListItemDto>>(cases));
//         }

//         // ── Commands ─────────────────────────────────────────────────────────────

//         public async Task<ApiResponse<UrgentCaseDetailDto>> CreateAsync(UrgentCaseCreateDto dto)
//         {
//             var userId = GetCurrentUserId();
//             if (userId is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Unauthorized.");

//             var user = await _userManager.FindByIdAsync(userId);
//             if (user is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("User not found.");

//             // ── Rate-limit: one urgent case per 14 days ───────────────────────
//             var cutoff = DateTime.UtcNow.AddDays(-RateLimitDays);
//             bool hasRecentCase = await _uow.Repository<UrgentCase>()
//                 .Query(tracked: false)
//                 .AnyAsync(c => c.UserId == userId && c.CreatedAt >= cutoff);

//             if (hasRecentCase)
//             {
//                 Log.Warning("User {UserId} hit the 14-day rate limit for urgent case creation.", userId);
//                 return ApiResponse<UrgentCaseDetailDto>.Fail(
//                     $"You cannot create more than one urgent case within {RateLimitDays} days.");
//             }

//             // ── Location from stored user profile ─────────────────────────────
//             if (user.CurrentLocationLatitude is null || user.CurrentLocationLongitude is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail(
//                     "Your current location is not available. Please update your location first.");

//             var location = new Point(
//                 user.CurrentLocationLongitude.Value,   // X = longitude
//                 user.CurrentLocationLatitude.Value)    // Y = latitude
//             { SRID = 4326 };

//             var now = DateTime.UtcNow;

//             var urgentCase = new UrgentCase
//             {
//                 Gender             = dto.Gender,
//                 Government         = dto.Government,
//                 City               = dto.City,
//                 Street             = dto.Street,
//                 FName              = dto.FName,
//                 SName              = dto.SName,
//                 TName              = dto.TName,
//                 LName              = dto.LName,
//                 Age                = dto.Age,
//                 CommunicationPhone = dto.CommunicationPhone,
//                 Relation           = dto.Relation,
//                 EventDate          = dto.EventDate,
//                 Description        = dto.Description,
//                 AgeCategoryId      = dto.AgeCategoryId,
//                 Location           = location,
//                 UserId             = userId,
//                 CaseCode           = GenerateCaseCode(),
//                 Status             = CaseStatus.Approved,    // published immediately
//                 CaseType           = CaseType.Urgent,
//                 CreatedAt          = now,
//                 EndDate            = now.AddHours(ExpirationHours),
//                 LimitReachDate     = now.AddDays(RateLimitDays)
//             };

//             // ── Upload photos ─────────────────────────────────────────────────
//             if (dto.Photos is { Count: > 0 })
//             {
//                 var photos = new List<CasePhoto>();
//                 for (int i = 0; i < dto.Photos.Count; i++)
//                 {
//                     var url = await _fileStorage.UploadAsync(dto.Photos[i]);
//                     photos.Add(new CasePhoto { Url = url, IsMain = i == 0 });
//                 }
//                 urgentCase.Photos = photos;
//             }

//             await _uow.Repository<UrgentCase>().CreateAsync(urgentCase);
//             await _uow.SaveAsync();

//             Log.Information("Urgent case {CaseCode} created by user {UserId}.", urgentCase.CaseCode, userId);

//             // ── Fire-and-forget notification ──────────────────────────────────
//             _ = Task.Run(async () =>
//             {
//                 try   { await _notificationService.NotifyNearbyUsersAsync(urgentCase, NotificationRadiusMeters); }
//                 catch (Exception ex) { Log.Error(ex, "Nearby notification failed for case {CaseCode}.", urgentCase.CaseCode); }
//             });

//             // Re-fetch with includes for the response
//             var created = await BaseQuery().FirstOrDefaultAsync(c => c.Id == urgentCase.Id);
//             return ApiResponse<UrgentCaseDetailDto>.Ok(
//                 _mapper.Map<UrgentCaseDetailDto>(created),
//                 "Urgent case created successfully.");
//         }

//         public async Task<ApiResponse<UrgentCaseDetailDto>> UpdateAsync(UrgentCaseUpdateDto dto)
//         {
//             var userId = GetCurrentUserId();
//             if (userId is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Unauthorized.");

//             // Fetch tracked for update
//             var urgentCase = await _uow.Repository<UrgentCase>()
//                 .GetOneAsync(c => c.Id == dto.Id, tracked: true);

//             if (urgentCase is null)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Urgent case not found.");

//             if (urgentCase.UserId != userId)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("You are not authorized to update this case.");

//             if (urgentCase.Status == CaseStatus.Founded)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Founded cases cannot be updated.");

//             if (urgentCase.DeletedAt.HasValue)
//                 return ApiResponse<UrgentCaseDetailDto>.Fail("Deleted cases cannot be updated.");

//             // Partial update — only overwrite fields that were sent
//             if (dto.Government         is not null) urgentCase.Government         = dto.Government;
//             if (dto.City               is not null) urgentCase.City               = dto.City;
//             if (dto.Street             is not null) urgentCase.Street             = dto.Street;
//             if (dto.FName              is not null) urgentCase.FName              = dto.FName;
//             if (dto.SName              is not null) urgentCase.SName              = dto.SName;
//             if (dto.TName              is not null) urgentCase.TName              = dto.TName;
//             if (dto.LName              is not null) urgentCase.LName              = dto.LName;
//             if (dto.Age.HasValue)                   urgentCase.Age                = dto.Age.Value;
//             if (dto.CommunicationPhone is not null) urgentCase.CommunicationPhone = dto.CommunicationPhone;
//             if (dto.Relation.HasValue)              urgentCase.Relation           = dto.Relation.Value;
//             if (dto.EventDate.HasValue)             urgentCase.EventDate          = dto.EventDate.Value;
//             if (dto.Description        is not null) urgentCase.Description        = dto.Description;
//             if (dto.AgeCategoryId.HasValue)         urgentCase.AgeCategoryId      = dto.AgeCategoryId.Value;

//             urgentCase.UpdatedAt = DateTime.UtcNow;

//             _uow.Repository<UrgentCase>().Update(urgentCase);
//             await _uow.SaveAsync();

//             Log.Information("Urgent case {CaseId} updated by user {UserId}.", dto.Id, userId);

//             var updated = await BaseQuery().FirstOrDefaultAsync(c => c.Id == dto.Id);
//             return ApiResponse<UrgentCaseDetailDto>.Ok(
//                 _mapper.Map<UrgentCaseDetailDto>(updated),
//                 "Urgent case updated successfully.");
//         }

//         public async Task<ApiResponse<bool>> DeleteAsync(long id)
//         {
//             var userId = GetCurrentUserId();
//             if (userId is null)
//                 return ApiResponse<bool>.Fail("Unauthorized.");

//             var urgentCase = await _uow.Repository<UrgentCase>()
//                 .GetOneAsync(c => c.Id == id, tracked: true);

//             if (urgentCase is null)
//                 return ApiResponse<bool>.Fail("Urgent case not found.");

//             if (urgentCase.UserId != userId)
//                 return ApiResponse<bool>.Fail("You are not authorized to delete this case.");

//             if (urgentCase.Status == CaseStatus.Founded)
//                 return ApiResponse<bool>.Fail("Founded cases cannot be deleted.");

//             urgentCase.DeletedAt      = DateTime.UtcNow;
//             urgentCase.PreviousStatus = urgentCase.Status;
//             urgentCase.Status         = CaseStatus.Deleted;

//             _uow.Repository<UrgentCase>().Update(urgentCase);
//             await _uow.SaveAsync();

//             Log.Information("Urgent case {CaseId} soft-deleted by user {UserId}.", id, userId);
//             return ApiResponse<bool>.Ok(true, "Urgent case deleted successfully.");
//         }

//         public async Task<ApiResponse<bool>> PermanentDeleteAsync(long id)
//         {
//             var urgentCase = await _uow.Repository<UrgentCase>()
//                 .GetOneAsync(c => c.Id == id, tracked: true);

//             if (urgentCase is null)
//                 return ApiResponse<bool>.Fail("Urgent case not found.");

//             _uow.Repository<UrgentCase>().Remove(urgentCase);
//             await _uow.SaveAsync();

//             Log.Information("Urgent case {CaseId} permanently deleted.", id);
//             return ApiResponse<bool>.Ok(true, "Urgent case permanently deleted.");
//         }

//         public async Task<ApiResponse<bool>> ApproveAsync(long id)
//         {
//             var urgentCase = await _uow.Repository<UrgentCase>()
//                 .GetOneAsync(c => c.Id == id, tracked: true);

//             if (urgentCase is null)
//                 return ApiResponse<bool>.Fail("Urgent case not found.");

//             if (urgentCase.Status == CaseStatus.Founded)
//                 return ApiResponse<bool>.Fail("Founded cases cannot be approved.");

//             if (urgentCase.DeletedAt.HasValue)
//                 return ApiResponse<bool>.Fail("Deleted cases cannot be approved.");

//             urgentCase.Status    = CaseStatus.Approved;
//             urgentCase.UpdatedAt = DateTime.UtcNow;

//             _uow.Repository<UrgentCase>().Update(urgentCase);
//             await _uow.SaveAsync();

//             Log.Information("Urgent case {CaseId} approved.", id);
//             return ApiResponse<bool>.Ok(true, "Urgent case approved.");
//         }

//         public async Task<ApiResponse<bool>> RejectAsync(long id)
//         {
//             var urgentCase = await _uow.Repository<UrgentCase>()
//                 .GetOneAsync(c => c.Id == id, tracked: true);

//             if (urgentCase is null)
//                 return ApiResponse<bool>.Fail("Urgent case not found.");

//             if (urgentCase.Status == CaseStatus.Founded)
//                 return ApiResponse<bool>.Fail("Founded cases cannot be rejected.");

//             if (urgentCase.DeletedAt.HasValue)
//                 return ApiResponse<bool>.Fail("Deleted cases cannot be rejected.");

//             urgentCase.Status    = CaseStatus.Rejected;
//             urgentCase.UpdatedAt = DateTime.UtcNow;

//             _uow.Repository<UrgentCase>().Update(urgentCase);
//             await _uow.SaveAsync();

//             Log.Information("Urgent case {CaseId} rejected.", id);
//             return ApiResponse<bool>.Ok(true, "Urgent case rejected.");
//         }

//         public async Task<ApiResponse<bool>> MarkAsFoundedAsync(long id)
//         {
//             var userId = GetCurrentUserId();

//             var urgentCase = await _uow.Repository<UrgentCase>()
//                 .GetOneAsync(c => c.Id == id, tracked: true);

//             if (urgentCase is null)
//                 return ApiResponse<bool>.Fail("Urgent case not found.");

//             if (urgentCase.Status == CaseStatus.Founded)
//                 return ApiResponse<bool>.Fail("Case is already marked as founded.");

//             if (urgentCase.DeletedAt.HasValue)
//                 return ApiResponse<bool>.Fail("Deleted cases cannot be marked as founded.");

//             urgentCase.Status    = CaseStatus.Founded;
//             urgentCase.UpdatedAt = DateTime.UtcNow;

//             _uow.Repository<UrgentCase>().Update(urgentCase);
//             await _uow.SaveAsync();

//             Log.Information("Urgent case {CaseId} marked as founded by user {UserId}.", id, userId);
//             return ApiResponse<bool>.Ok(true, "Case marked as founded.");
//         }
//     }
// }