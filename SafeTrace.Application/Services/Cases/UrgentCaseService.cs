using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.UrgentCase.Response;
using SafeTrace.Application.DTOs.UrgentCase.Request;

namespace SafeTrace.Application.Services.Cases
{
    public class UrgentCaseService : BaseCasesService<UrgentCase, UrgentCaseListDto, UrgentCaseDetailDto, UrgentCasesFilterDto>, IUrgentCaseService
    {
        private readonly INotificationServices _notificationServices;
        private const string FolderName = "UrgentCases";
        private const int RateLimitDays = 14;
        private const int ExpirationHours = 48;
        private const double NotifyRadiusM = 50_000; // 50 km
        private const string NotificationBaseUrl = "/urgent-cases/detail/";
        
        public UrgentCaseService(
            ILogger<UrgentCaseService> logger,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
            INotificationServices notificationServices)
            : base(unitOfWork, mapper, caseHelper, logger)
        {
            _notificationServices = notificationServices;
        }

        /// <summary>
        /// Filters urgent cases by geographic location and radius.
        /// </summary>
        protected override Task OnMarkedAsFoundAsync(UrgentCase entity)
        {
            entity.EndDate = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Filters urgent cases by geographic location and radius.
        /// </summary>
        protected override IQueryable<UrgentCase> ApplyCustomFilter(IQueryable<UrgentCase> query, UrgentCasesFilterDto filter)
        {
            if (!filter.Latitude.HasValue && !filter.Longitude.HasValue)
                return query;

            var location = CreateUserLocation(filter);

            return query.Where(x => x.Location.Distance(location) <= filter.RadiusInMeters);
        }

        /// <summary>
        /// Sorts urgent cases by distance from the user's location.
        /// </summary>
        protected override IQueryable<UrgentCase> ApplyCustomSorting(IQueryable<UrgentCase> query, UrgentCasesFilterDto filter)
        {
            if (!filter.Latitude.HasValue && !filter.Longitude.HasValue)
                return query;

            var location = CreateUserLocation(filter);

            return query.OrderBy(x => x.Location.Distance(location));
        }

        /// <summary>
        /// Creates a new urgent case with expiration and rate limiting.
        /// </summary>
        public async Task<ApiResponse<string>> CreateAsync(string userId, UrgentCaseCreateDto dto)
        {
            var rateLimitViolation = await CheckRateLimitAsync(userId);

            if (rateLimitViolation is not null)
                return ApiResponse<string>.Fail(rateLimitViolation);

            var now = DateTime.UtcNow;
            var entity = _mapper.Map<UrgentCase>(dto);

            entity.UserId = userId;
            entity.CaseType = CaseType.Urgent;
            entity.Status = CaseStatus.Active;
            entity.CreatedAt = now;
            entity.LimitReachDate = now.AddDays(RateLimitDays);
            entity.EndDate = now.AddHours(ExpirationHours);
            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.URG);
            entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
            entity.Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 };

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    entity.CaseFiles = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        FolderName);

                    await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);

                    return true;
                },
                onFailureAsync: ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(entity.CaseFiles.Select(p => p.ImagePath));
                    _logger.LogError(ex, "Failed to create urgent case for user {UserId}", userId);
                    return Task.CompletedTask;
                });
                
            _logger.LogInformation(
                "Urgent case {CaseCode} created by user {UserId}. EndDate: {EndDate}",
                entity.CaseCode, userId, entity.EndDate);

            _ = NotifyNearbyUsersAsync(entity);

            return ApiResponse<string>.Ok(message: "تم إنشاء الحالة العاجلة بنجاح.");
        }
        
        /// <summary>
        /// Updates an existing urgent case with new photos and location.
        /// </summary>
        public async Task<ApiResponse<string>> UpdateAsync(long id, string userId, UrgentCaseUpdateDto updateDto)
        {
            var entity = await _caseHelper.GetValidCaseAsync<UrgentCase>(
                id,
                userId,
                checkOwnership: true,
                includes: [x => x.CaseFiles]);

            _caseHelper.ValidateCaseIsEditable(entity);

            var uploadedPhotos = new List<CaseFile>();
            var filesToDelete = new List<string>();

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    if (updateDto.NewPhotos?.Count > 0)
                    {
                        var primaryImage = updateDto.NewPhotos[0];
                        var additionalImages = updateDto.NewPhotos.Skip(1);

                        uploadedPhotos = await _caseHelper.CreateCaseFilesAsync(
                            primaryImage, additionalImages, null, FolderName, entity.Id);

                        foreach (var photo in uploadedPhotos)
                            entity.CaseFiles.Add(photo);
                    }

                    if (updateDto.DeletedPhotoIds?.Count > 0)
                    {
                        var toRemove = entity.CaseFiles
                            .Where(p => updateDto.DeletedPhotoIds.Contains(p.Id))
                            .ToList();

                        filesToDelete.AddRange(toRemove.Select(p => p.ImagePath));

                        foreach (var photo in toRemove)
                            entity.CaseFiles.Remove(photo);
                    }

                    _mapper.Map(updateDto, entity);
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

                    if (updateDto.Latitude.HasValue && updateDto.Longitude.HasValue)
                    {
                        entity.Location = new Point(updateDto.Longitude.Value, updateDto.Latitude.Value) { SRID = 4326 };
                    }

                    _unitOfWork.Repository<UrgentCase>().Update(entity);

                    return true;
                },
                onFailureAsync: ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(uploadedPhotos.Select(p => p.ImagePath));
                    _logger.LogError(ex, "Failed to update urgent case {CaseId} for user {UserId}", id, userId);
                    return Task.CompletedTask;
                });

            _logger.LogInformation("Urgent case {CaseId} updated by user {UserId}.", entity.Id, userId);

            _caseHelper.CleanupPhysicalFiles(filesToDelete);

            return ApiResponse<string>.Ok(message: "تم تحديث الحالة العاجلة بنجاح.");
        }
                
        private static Point CreateUserLocation(UrgentCasesFilterDto filter)
        {
            return new Point(filter.Longitude!.Value,filter.Latitude!.Value){ SRID = 4326 };
        }

        private async Task<string?> CheckRateLimitAsync(string userId)
        {
            var now = DateTime.UtcNow;

            var lastCase = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.CaseCode, x.Status, x.CreatedAt, x.LimitReachDate })
                .FirstOrDefaultAsync();

            if (lastCase is null || lastCase.LimitReachDate <= now)
                return null;

            var remaining = (int)Math.Ceiling((lastCase.LimitReachDate - now).TotalDays);

            _logger.LogInformation(
                "Rate limit hit for user {UserId}. Last case: {CaseCode} ({Status}). " +
                "LimitReachDate: {LimitReachDate}. Remaining: {Days} day(s).",
                userId, lastCase.CaseCode, lastCase.Status,
                lastCase.LimitReachDate, remaining);

            return remaining == 1
            ? "لا يمكنك إنشاء حالة عاجلة جديدة الآن. يمكنك المحاولة مرة أخرى بعد يوم واحد."
            : $"لا يمكنك إنشاء حالة عاجلة جديدة الآن. يمكنك المحاولة مرة أخرى بعد {remaining} أيام.";
        }

        private async Task NotifyNearbyUsersAsync(UrgentCase entity)
        {
            try
            {
                var caseLocation = entity.Location;

                var nearbyUserIds = await _unitOfWork.Repository<ApplicationUser>()
                    .Query(tracked: false)
                    .Where(u =>
                        u.Id != entity.UserId &&
                        (
                            (u.CurrentLocationLatitude != null &&
                             u.CurrentLocationLongitude != null &&
                             new Point(u.CurrentLocationLongitude.Value, u.CurrentLocationLatitude.Value) { SRID = 4326 }
                                .Distance(caseLocation) <= NotifyRadiusM)
                            ||
                            (u.HomeLocationLatitude != null &&
                             u.HomeLocationLongitude != null &&
                             new Point(u.HomeLocationLongitude.Value, u.HomeLocationLatitude.Value) { SRID = 4326 }
                                .Distance(caseLocation) <= NotifyRadiusM)
                        ))
                    .Select(u => u.Id)
                    .ToListAsync();

                if (nearbyUserIds.Count == 0)
                {
                    return;
                }

                var notificationContent =
                    $"🚨 توجد حالة عاجلة بالقرب منك.\n" +
                    $"كود الحالة: {entity.CaseCode}\n" +
                    $"العمر: {entity.Age}\n" +
                    $"آخر مكان: {entity.City} - {entity.Government}";

                using var semaphore = new SemaphoreSlim(10);

                var tasks = nearbyUserIds.Select(async recipientId =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await _notificationServices.SendNotificationAsync(new SendNotificationDTO
                        {
                            UserId = recipientId,
                            Content = notificationContent,
                            Type = NotificationType.Message,
                            NotificationDirectLink = NotificationBaseUrl + entity.Id
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to notify user {UserId} about case {CaseCode}.",
                            recipientId, entity.CaseCode);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                _logger.LogInformation("Finished notifying nearby users for urgent case {CaseCode}.", entity.CaseCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyNearbyUsersAsync failed for urgent case {CaseCode}.", entity.CaseCode);
            }
        }
    
    }
}