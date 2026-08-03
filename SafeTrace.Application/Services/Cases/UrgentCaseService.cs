using NetTopologySuite.Geometries;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.UrgentCase.Request;
using SafeTrace.Application.DTOs.UrgentCase.Response;
using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;

namespace SafeTrace.Application.Services.Cases
{
    public class UrgentCaseService : BaseCasesService<UrgentCase, UrgentCaseListDto, UrgentCaseDetailDto, UrgentCasesFilterDto>, IUrgentCaseService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private const string FolderName = "UrgentCases";
        private const int RateLimitDays = 2;
        private const int ExpirationHours = 48;
        private const double NotifyRadiusM = 500_000; // 500 km
        private const double LocationShiftNotificationThresholdMeters = 5000; // 5 km

        private const string detailsUrl = EmailTemplates.UrgentCaseDetailsRoute;

        public UrgentCaseService(
            ILogger<UrgentCaseService> logger,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
            IServiceScopeFactory scopeFactory)
            : base(unitOfWork, mapper, caseHelper, logger)
        {
            _scopeFactory = scopeFactory;
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
            if (!filter.Latitude.HasValue || !filter.Longitude.HasValue || !filter.RadiusInKm.HasValue)
                return query;

            var location = CreateUserLocation(filter);
            var radiusInMeters = filter.RadiusInKm.Value * 1000;

            return query.Where(x => x.Location.Distance(location) <= radiusInMeters);
        }

        /// <summary>
        /// Sorts urgent cases by distance from the user's location.
        /// </summary>
        protected override IQueryable<UrgentCase> ApplyCustomSorting(IQueryable<UrgentCase> query, UrgentCasesFilterDto filter)
        {
            if (!filter.Latitude.HasValue || !filter.Longitude.HasValue)
                return query;

            var location = CreateUserLocation(filter);

            return query.OrderBy(x => x.Location.Distance(location));
        }

        /// <summary>
        /// Creates a new urgent case with expiration and rate limiting.
        /// </summary>
        public async Task<ApiResponse<CreateCaseResponseDto>> CreateAsync(string userId, UrgentCaseCreateDto dto, bool forceCreate = false)
        {
            var rateLimitViolation = await CheckRateLimitAsync(userId);
            if (rateLimitViolation is not null)
            {
               return ApiResponse<CreateCaseResponseDto>.Fail(rateLimitViolation);
            }

            await _caseHelper.ValidateUploadedImagesIdentityAsync(dto.PrimaryImage, dto.AdditionalImages);

            var duplicateCheck = await _caseHelper.CheckDuplicateCaseAsync(CaseType.Urgent, dto.PrimaryImage, userId);

            if (duplicateCheck.IsBlocked)
            {
                return ApiResponse<CreateCaseResponseDto>.Ok(
                    new CreateCaseResponseDto
                    {
                        IsCreated = false,
                        IsBlocked = true,
                        DuplicateDecision = duplicateCheck.DuplicateDecision,
                        ExistingCaseId = duplicateCheck.ExistingCaseId,
                        ExistingCaseType = duplicateCheck.ExistingCaseType,
                        ExistingStatus = duplicateCheck.ExistingStatus,
                        MatchedCases = duplicateCheck.MatchedCases
                    });
            }

            if (duplicateCheck.DuplicateDecision != DuplicateDecision.None && !forceCreate)
            {
                return ApiResponse<CreateCaseResponseDto>.Ok(
                    new CreateCaseResponseDto
                    {
                        IsCreated = false,
                        IsBlocked = false,
                        DuplicateDecision = duplicateCheck.DuplicateDecision,
                        ExistingCaseId = duplicateCheck.ExistingCaseId,
                        ExistingCaseType = duplicateCheck.ExistingCaseType,
                        ExistingStatus = duplicateCheck.ExistingStatus,
                        MatchedCases = duplicateCheck.MatchedCases
                    });
            }

            var now = DateTime.UtcNow;

            var entity = _mapper.Map<UrgentCase>(dto);

            entity.UserId = userId;
            entity.CaseType = CaseType.Urgent;
            entity.Status = CaseStatus.Active;
            entity.CreatedAt = now;
            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.URG);
            entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

            entity.LimitReachDate = now.AddMinutes(RateLimitDays);
            entity.EndDate = now.AddHours(ExpirationHours);
            entity.Location = new Point(dto.Longitude, dto.Latitude){ SRID = 4326 };

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    var uploadedFiles = await _caseHelper.CreateCaseFilesAsync(
                        dto.PrimaryImage,
                        dto.AdditionalImages,
                        dto.Video,
                        FolderName,
                        entity.Id);

                    foreach (var file in uploadedFiles)
                        entity.CaseFiles.Add(file);

                    await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);

                    return true;
                },
                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(entity.CaseFiles.Select(x => x.ImagePath));

                    await _caseHelper.DeleteFacesAsync(entity.CaseFiles.Select(x => x.FaceId), entity.Id);

                    _logger.LogError(
                        ex,
                        "Failed to create Urgent case for user {UserId}",
                        userId);
                });

            _logger.LogInformation(
                "Created Urgent case. CaseId={CaseId}, CaseCode={CaseCode}, UserId={UserId}",
                entity.Id,
                entity.CaseCode,
                userId);
            
            await NotifyNearbyUsersAsync(entity);

            return ApiResponse<CreateCaseResponseDto>.Ok(
                new CreateCaseResponseDto
                {
                    IsCreated = true,
                    IsBlocked = false,
                    CaseId = entity.Id,
                    MatchedCases = []
                });
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

            var existingFaceIds = entity.CaseFiles?
                .Where(f => !string.IsNullOrWhiteSpace(f.FaceId))
                .Select(f => f.FaceId!)
                .ToList();

            await _caseHelper.ValidateUploadedImagesIdentityAsync(updateDto.PrimaryImage, updateDto.NewPhotos, existingFaceIds);

            var uploadedPhotos = new List<CaseFile>();
            var filesToDelete = new List<string>();

            // ── FIX: this list didn't exist before. Without it, deleted/replaced
            // photos were removed from S3/disk but their FaceId NEVER got removed
            // from the AI matching (vector) database — the exact issue you were
            // originally worried about.
            var faceIdsToDelete = new List<string>();
            bool locationShifted = false;

            await ExecuteInTransactionAsync(
                action: async () =>
                {
                    // Moved BEFORE NewPhotos so that, if the old primary photo is also
                    // being replaced via PrimaryImage below, we can correctly detect
                    // it was already deleted here and avoid double-deleting it.
                    if (updateDto.DeletedPhotoIds?.Count > 0)
                    {
                        var toRemove = entity.CaseFiles
                            .Where(p => updateDto.DeletedPhotoIds.Contains(p.Id))
                            .ToList();

                        filesToDelete.AddRange(toRemove.Select(p => p.ImagePath));

                        // ── FIX: face ids were never captured for deletion before.
                        faceIdsToDelete.AddRange(
                            toRemove
                                .Where(p => !string.IsNullOrWhiteSpace(p.FaceId))
                                .Select(p => p.FaceId!));

                        foreach (var photo in toRemove)
                            entity.CaseFiles.Remove(photo);
                    }

                    if (updateDto.NewPhotos?.Count > 0)
                    {
                        var primaryImage = updateDto.NewPhotos[0];
                        var additionalImages = updateDto.NewPhotos.Skip(1);

                        var newPhotos = await _caseHelper.CreateCaseFilesAsync(
                            primaryImage, additionalImages, null, FolderName, entity.Id);

                        uploadedPhotos.AddRange(newPhotos);

                        foreach (var photo in newPhotos)
                            entity.CaseFiles.Add(photo);
                    }

                    // ── FIX: PrimaryImage (crop-and-replace primary photo from the edit
                    // form) was never handled — the frontend sends it, but nothing ever
                    // consumed it. Takes precedence over PrimaryPhotoId, matching the
                    // frontend behavior (it clears primaryPhotoId whenever a new
                    // cropped/replacement primary photo is confirmed).
                    if (updateDto.PrimaryImage is not null)
                    {
                        var oldPrimary = entity.CaseFiles.FirstOrDefault(p => p.IsPrimary);

                        var newPrimaryPhotos = await _caseHelper.CreateCaseFilesAsync(
                            updateDto.PrimaryImage,
                            null,
                            null,
                            FolderName,
                            entity.Id);

                        uploadedPhotos.AddRange(newPrimaryPhotos);
                        var newPrimary = newPrimaryPhotos.First();

                        if (oldPrimary is not null)
                        {
                            filesToDelete.Add(oldPrimary.ImagePath);

                            if (!string.IsNullOrWhiteSpace(oldPrimary.FaceId))
                                faceIdsToDelete.Add(oldPrimary.FaceId);

                            entity.CaseFiles.Remove(oldPrimary);
                        }

                        foreach (var f in entity.CaseFiles)
                            f.IsPrimary = false;

                        newPrimary.IsPrimary = true;
                        entity.CaseFiles.Add(newPrimary);
                    }
                    // ── FIX: PrimaryPhotoId (marking an existing photo as primary,
                    // e.g. clicking "اجعلها أساسية") was never handled either.
                    else if (updateDto.PrimaryPhotoId.HasValue)
                    {
                        _caseHelper.SetPrimaryImage(entity.CaseFiles, updateDto.PrimaryPhotoId.Value);
                    }

                    _mapper.Map(updateDto, entity);
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

                    if (updateDto.Latitude.HasValue && updateDto.Longitude.HasValue)
                    {
                        var newLat = updateDto.Latitude.Value;
                        var newLon = updateDto.Longitude.Value;

                        if (entity.Location != null)
                        {
                            var oldLat = entity.Location.Y;
                            var oldLon = entity.Location.X;

                            var distanceMeters = CalculateDistanceMeters(oldLat, oldLon, newLat, newLon);
                            if (distanceMeters >= LocationShiftNotificationThresholdMeters)
                            {
                                locationShifted = true;
                            }
                        }

                        entity.Location = new Point(newLon, newLat) { SRID = 4326 };
                    }

                    _unitOfWork.Repository<UrgentCase>().Update(entity);

                    return true;
                },
                onFailureAsync: async ex =>
                {
                    _caseHelper.CleanupPhysicalFiles(uploadedPhotos.Select(p => p.ImagePath));

                    // ── FIX: on failure, newly-indexed faces (from NewPhotos or
                    // PrimaryImage) were never cleaned up from the vector db either.
                    await _caseHelper.DeleteFacesAsync(
                        uploadedPhotos
                            .Where(x => !string.IsNullOrWhiteSpace(x.FaceId))
                            .Select(x => x.FaceId!),
                        entity.Id);

                    _logger.LogError(ex, "Failed to update urgent case {CaseId} for user {UserId}", id, userId);
                });

            if (locationShifted && entity.Status == CaseStatus.Active)
            {
                await NotifyNearbyUsersAsync(entity);
            }

            _logger.LogInformation("Urgent case {CaseId} updated by user {UserId}.", entity.Id, userId);

            _caseHelper.CleanupPhysicalFiles(filesToDelete);

            // ── FIX: this whole block was missing — deleted/replaced photos' faces
            // never got removed from the AI matching database.
            if (faceIdsToDelete.Count > 0)
                await _caseHelper.DeleteFacesAsync(faceIdsToDelete, entity.Id);

            return ApiResponse<string>.Ok(message: "تم تحديث الحالة العاجلة بنجاح.");
        }

        /// <summary>
        /// Retrieves the user's urgent case creation status and the remaining time before they are allowed to create a new urgent case.
        /// </summary>
        public async Task<ApiResponse<UrgentCreationStatusResponse>> GetUrgentCreationStatusAsync(string userId)
        {
            var now = DateTime.UtcNow;

            var lastCase = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.LimitReachDate
                })
                .FirstOrDefaultAsync();

            if (lastCase == null)
            {
                return ApiResponse<UrgentCreationStatusResponse>.Ok(
                    new UrgentCreationStatusResponse
                    {
                        IsAllowed = true,
                        RemainingMinutes = 0
                    });
            }

            if (lastCase.LimitReachDate <= now)
            {
                return ApiResponse<UrgentCreationStatusResponse>.Ok(
                    new UrgentCreationStatusResponse
                    {
                        IsAllowed = true,
                        RemainingMinutes = 0
                    });
            }

            var remainingMinutes = (int)Math.Ceiling((lastCase.LimitReachDate - now).TotalMinutes);

            return ApiResponse<UrgentCreationStatusResponse>.Ok(
                new UrgentCreationStatusResponse
                {
                    IsAllowed = false,
                    RemainingMinutes = remainingMinutes
                });
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

            var remaining = (int)Math.Ceiling((lastCase.LimitReachDate - now).TotalMinutes);

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

                var nearbyUsers = await _unitOfWork.Repository<ApplicationUser>()
                    .Query(tracked: false)
                    .Where(u =>
                        u.Id != entity.UserId &&
                        (
                            (u.CurrentLocation != null &&
                            u.CurrentLocation.Distance(caseLocation) <= NotifyRadiusM)
                            ||
                            (u.HomeLocation != null &&
                            u.HomeLocation.Distance(caseLocation) <= NotifyRadiusM)
                        ))
                    .Select(u => new
                    {
                        u.Id,
                        u.Email
                    })
                    .ToListAsync();

                if (!nearbyUsers.Any())
                    return;

                var notificationContent =
                    $"🚨 توجد حالة عاجلة بالقرب منك.\n" +
                    $"كود الحالة: {entity.CaseCode}\n" +
                    $"العمر: {entity.Age}\n" +
                    $"آخر مكان: {entity.City} - {entity.Government}";

                var subject = "🚨 حالة عاجلة بالقرب منك";

                using var semaphore = new SemaphoreSlim(10);

                var tasks = nearbyUsers.Select(async user =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var notificationServices = scope.ServiceProvider.GetRequiredService<INotificationServices>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        // Send Notification
                        await notificationServices.SendNotificationAsync(new SendNotificationDTO
                        {
                            UserId = user.Id,
                            Content = notificationContent,
                            Type = NotificationType.Message,
                            NotificationDirectLink = detailsUrl + entity.Id
                        });
                        // Send Email
                        if (!string.IsNullOrWhiteSpace(user.Email))
                        {
                            var body = EmailTemplates.BuildUrgentCaseNotificationEmailTemplate(
                                caseName: $"{entity.FName} {entity.SName} {entity.TName} {entity.LName}".Trim(),
                                caseCode: entity.CaseCode,
                                age: entity.Age,
                                gender: entity.Gender,
                                government: entity.Government,
                                city: entity.City,
                                publishedAt: entity.CreatedAt,
                                detailsUrl: EmailTemplates.GetCaseDetailsUrl(entity.CaseType, entity.Id));

                            await emailService.SendEmailAsync(
                                user.Email,
                                subject,
                                body);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to notify user {UserId} for urgent case {CaseCode}.",
                            user.Id,
                            entity.CaseCode);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "NotifyNearbyUsersAsync failed for urgent case {CaseCode}.",
                    entity.CaseCode);
            }
        }

        private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth mean radius in meters
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
