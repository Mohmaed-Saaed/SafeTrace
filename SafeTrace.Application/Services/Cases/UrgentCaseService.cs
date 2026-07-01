using SafeTrace.Application.Exceptions;
using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.UrgentCase.Response;
using SafeTrace.Application.DTOs.UrgentCase.Request;

namespace SafeTrace.Application.Services.Cases
{
    internal class UrgentCaseService : IUrgentCaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICaseHelperService _caseHelper;
        private readonly INotificationServices _notificationServices;
        private readonly ILogger<UrgentCaseService> _logger;

        private const int RateLimitDays = 14;
        private const int ExpirationHours = 48;
        private const double NotifyRadiusM = 50_000; // 50 km

        public UrgentCaseService(
            ILogger<UrgentCaseService> logger,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseHelperService caseHelper,
            INotificationServices notificationServices)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _caseHelper = caseHelper;
            _notificationServices = notificationServices;
        }

        public async Task<ApiResponse<UrgentCaseCreateResponse>> CreateAsync(string userId, UrgentCaseCreateDto createDto)
        {
            var rateLimitViolation = await CheckRateLimitAsync(userId);

            if (rateLimitViolation is not null)
                return ApiResponse<UrgentCaseCreateResponse>.Fail(rateLimitViolation);

            // ── Build entity ─────────────────────────────────────────────
            var now = DateTime.UtcNow;
            var entity = _mapper.Map<UrgentCase>(createDto);

            entity.UserId = userId;
            entity.CaseType = CaseType.Urgent;
            entity.Status = CaseStatus.Active;
            entity.CreatedAt = now;
            entity.LimitReachDate = now.AddDays(RateLimitDays);
            entity.EndDate = now.AddHours(ExpirationHours);
            entity.CaseCode = await _caseHelper.GenerateCaseCodeAsync(CaseCodePrefix.URG);
            entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);
            entity.Location = new Point(createDto.Longitude, createDto.Latitude) { SRID = 4326 };

            // ── Persist with file handling ────────────────────────────────
            var uploadedPhotos = new List<CasePhoto>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                uploadedPhotos = await _caseHelper.HandlePhotoUploadsAsync(createDto.Photos, "UrgentCases");
                _caseHelper.EnsureSinglePrimaryPhoto(uploadedPhotos);
                entity.Photos = uploadedPhotos;

                await _unitOfWork.Repository<UrgentCase>().CreateAsync(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Urgent case {CaseCode} created by user {UserId}. EndDate: {EndDate}",
                    entity.CaseCode, userId, entity.EndDate);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _caseHelper.CleanupPhysicalFilesAsync(uploadedPhotos.Select(p => p.ImagePath));

                _logger.LogError(ex, "Failed to create urgent case for user {UserId}", userId);
                throw;
            }

            // Runs AFTER commit so the case is visible in the DB.
            // Fire-and-forget — notification failure must never fail the create.
            _ = NotifyNearbyUsersAsync(entity, createDto.Latitude, createDto.Longitude);

            // ── Return populated DTO ──────────────────────────────────────
            var created = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false, includes:
                [
                    x => x.AgeCategory,
                    x => x.Photos,
                    x => x.User
                ])
                .FirstAsync(x => x.Id == entity.Id);

            return ApiResponse<UrgentCaseCreateResponse>.Ok(
                data: _mapper.Map<UrgentCaseCreateResponse>(created),
                message: "Urgent case created successfully.");
        }

        public async Task<ApiResponse<UrgentCaseUpdateResponse>> UpdateAsync(string userId, UrgentCaseUpdateDto updateDto)
        {
            UrgentCase entity;
            try
            {
                entity = await _caseHelper.GetValidCaseAsync<UrgentCase>(updateDto.Id, userId, checkOwnership: true, includes: [x => x.Photos]);
                _caseHelper.ValidateCaseIsEditable(entity);
            }
            catch (NotFoundException)
            {
                return ApiResponse<UrgentCaseUpdateResponse>.Fail("Urgent case not found.");
            }
            catch (UnauthorizedException)
            {
                return ApiResponse<UrgentCaseUpdateResponse>.Fail("You are not authorized to update this case.");
            }
            catch (BadRequestException ex)
            {
                return ApiResponse<UrgentCaseUpdateResponse>.Fail(ex.Message);
            }

            var uploadedPhotos = new List<CasePhoto>();
            var filesToDelete = new List<string>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // ── New photos ───────────────────────────────────────────
                if (updateDto.Photos?.Count > 0)
                {
                    uploadedPhotos = await _caseHelper.HandlePhotoUploadsAsync(updateDto.Photos, "UrgentCases", entity.Id);
                    foreach (var photo in uploadedPhotos)
                    {
                        entity.Photos.Add(photo);
                    }
                }

                // ── Remove selected photos ───────────────────────────────
                if (updateDto.DeletedPhotoIds?.Count > 0)
                {
                    var ownedIds = entity.Photos.Select(p => p.Id).ToHashSet();
                    var toRemove = entity.Photos.Where(p => updateDto.DeletedPhotoIds.Contains(p.Id) && ownedIds.Contains(p.Id)).ToList();

                    filesToDelete.AddRange(toRemove.Select(p => p.ImagePath));

                    foreach (var photo in toRemove)
                    {
                        entity.Photos.Remove(photo);
                    }
                }

                // Ensure single primary photo if primary was deleted
                _caseHelper.EnsureSinglePrimaryPhoto(entity.Photos);

                // ── Map scalar properties ────────────────────────────────
                _mapper.Map(updateDto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                // Re-resolve age category if age changed
                if (updateDto.Age.HasValue)
                    entity.AgeCategoryId = await _caseHelper.ResolveAgeCategoryIdAsync(entity.Age);

                _unitOfWork.Repository<UrgentCase>().Update(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Urgent case {CaseId} updated by user {UserId}.", entity.Id, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _caseHelper.CleanupPhysicalFilesAsync(uploadedPhotos.Select(p => p.ImagePath));

                _logger.LogError(ex, "Failed to update urgent case {CaseId} for user {UserId}", updateDto.Id, userId);
                throw;
            }

            // Delete physical files after successful commit
            await _caseHelper.CleanupPhysicalFilesAsync(filesToDelete);

            // ── Reload and return ────────────────────────────────────────
            var updated = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false, includes:
                [
                    x => x.AgeCategory,
                    x => x.Photos,
                    x => x.User
                ])
                .FirstAsync(x => x.Id == entity.Id);

            return ApiResponse<UrgentCaseUpdateResponse>.Ok(
                data: _mapper.Map<UrgentCaseUpdateResponse>(updated),
                message: "Urgent case updated successfully.");
        }

        private async Task<string?> CheckRateLimitAsync(string userId)
        {
            var lastCase = await _unitOfWork.Repository<UrgentCase>()
                .Query(tracked: false)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.CaseCode, x.Status, x.CreatedAt, x.LimitReachDate })
                .FirstOrDefaultAsync();

            if (lastCase is null || lastCase.LimitReachDate <= DateTime.UtcNow)
                return null;

            var remaining = (int)Math.Ceiling((lastCase.LimitReachDate - DateTime.UtcNow).TotalDays);

            _logger.LogInformation(
                "Rate limit hit for user {UserId}. Last case: {CaseCode} ({Status}). " +
                "LimitReachDate: {LimitReachDate}. Remaining: {Days} day(s).",
                userId, lastCase.CaseCode, lastCase.Status,
                lastCase.LimitReachDate, remaining);

            return $"You can create a new urgent case in {remaining} day(s).";
        }

        private async Task NotifyNearbyUsersAsync(UrgentCase entity, double caseLat, double caseLng)
        {
            try
            {
                var caseLocation = new Point(caseLng, caseLat) { SRID = 4326 };

                // Find all users who have a stored location within the radius.
                // We project to a minimal anonymous type to avoid loading the full
                // ApplicationUser entity for every nearby person.
                var nearbyUserIds = await _unitOfWork.Repository<ApplicationUser>()
                    .Query(tracked: false)
                    .Where(u =>
                        u.Id != entity.UserId &&   // don't notify the creator
                        (
                            // current location within radius
                            (u.CurrentLocationLatitude != null &&
                             u.CurrentLocationLongitude != null &&
                             new Point(u.CurrentLocationLongitude.Value, u.CurrentLocationLatitude.Value) { SRID = 4326 }
                                .Distance(caseLocation) <= NotifyRadiusM)
                            ||
                            // fall back to home location
                            (u.HomeLocationLatitude != null &&
                             u.HomeLocationLongitude != null &&
                             new Point(u.HomeLocationLongitude.Value, u.HomeLocationLatitude.Value) { SRID = 4326 }
                                .Distance(caseLocation) <= NotifyRadiusM)
                        ))
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!nearbyUserIds.Any())
                {
                    _logger.LogInformation("No nearby users to notify for urgent case {CaseCode}.", entity.CaseCode);
                    return;
                }

                _logger.LogInformation("Notifying {Count} nearby user(s) about urgent case {CaseCode}.", nearbyUserIds.Count, entity.CaseCode);

                // Build the notification content once and send to each nearby user.
                var notificationContent =
                    $"🚨 New urgent case nearby: {entity.CaseCode}. " +
                    $"Missing person aged {entity.Age}, " +
                    $"last seen in {entity.City}, {entity.Government}.";

                // Send in parallel — capped at 10 concurrent sends to avoid
                // overwhelming the hub or hitting rate limits.
                var semaphore = new SemaphoreSlim(10);

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
                            NotificationDirectLink = ""
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log per-user failure — don't abort the rest
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
                // Notification failure must NEVER propagate — the case is already saved.
                _logger.LogError(ex, "NotifyNearbyUsersAsync failed for urgent case {CaseCode}.", entity.CaseCode);
            }
        }
    }
}