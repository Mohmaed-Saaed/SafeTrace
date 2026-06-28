using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Common.Helpers;
using SafeTrace.Application.Common.Models;
using SafeTrace.Application.DTOs.LongTermCases;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Extensions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Application.Services
{
    public class LongTermCaseService : ILongTermCaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<LongTermCaseService> _logger;

        public LongTermCaseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorage,
            ILogger<LongTermCaseService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // READ
        // ─────────────────────────────────────────────────────────────

        public async Task<PagedResultDto<LongTermCaseCardDto>> GetAllAsync(LongTermCaseFilterDto filter)
        {
            var name = filter.Name?.Trim();
            var filterByAge = filter.AgeCategory.HasValue;
            var ageRange = filterByAge ? AgeCategoryHelper.GetRange(filter.AgeCategory!.Value) : (Min: 0, Max: 0);

            var query = _unitOfWork.Repository<LongTermMissingCase>()
                .Query(tracked: false, includes: c => c.Photos)
                .Where(c => c.Status == CaseStatus.Active)
                .WhereIf(!string.IsNullOrEmpty(name), c =>
                    (c.FName ?? "").Contains(name!) ||
                    (c.SName ?? "").Contains(name!) ||
                    (c.TName ?? "").Contains(name!) ||
                    (c.LName ?? "").Contains(name!))
                .WhereIf(filter.Gender.HasValue, c => c.Gender == filter.Gender!.Value)
                .WhereIf(filterByAge, c => c.Age >= ageRange.Min && c.Age <= ageRange.Max);

            var totalCount = await query.CountAsync();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize < 1 ? 12 : filter.PageSize;

            var items = filter.SortDescending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt);

            var paged = await items
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<LongTermCaseCardDto>
            {
                Items = _mapper.Map<IEnumerable<LongTermCaseCardDto>>(paged),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<LongTermCaseDetailsDto> GetByIdAsync(long id, bool includeDeleted = false)
        {
            var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                .GetOneAsync(
                    c => c.Id == id && (includeDeleted || c.Status != CaseStatus.Deleted),
                    tracked: false,
                    c => c.Photos,
                    c => c.FoundPersonInfo!,
                    c => c.User);

            if (entity is null)
                throw new NotFoundException($"Long-term case with id {id} was not found.");

            return _mapper.Map<LongTermCaseDetailsDto>(entity);
        }

        public async Task<IEnumerable<LongTermCaseCardDto>> GetMyCasesAsync(string userId)
        {
            var items = await _unitOfWork.Repository<LongTermMissingCase>()
                .Query(tracked: false, orderBy: c => c.CreatedAt, orderByDirection: OrderBy.Descending, includes: c => c.Photos)
                .Where(c => c.UserId == userId && c.Status != CaseStatus.Deleted)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(items);
        }

        public async Task<IEnumerable<LongTermCaseCardDto>> GetFoundedCasesAsync()
        {
            var items = await _unitOfWork.Repository<LongTermMissingCase>()
                .Query(tracked: false, orderBy: c => c.CreatedAt, orderByDirection: OrderBy.Descending, includes: c => c.Photos)
                .Where(c => c.Status == CaseStatus.Found)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(items);
        }

        /// <summary>
        /// Admin-only: returns Pending or Deleted cases based on the requested status.
        /// Replaces the old GetPendingCasesAsync / GetDeletedCasesAsync pair.
        /// </summary>
        public async Task<IEnumerable<LongTermCaseCardDto>> GetAdminCasesAsync(CaseStatus status)
        {
            // Only Pending and Deleted are valid admin-filter statuses
            if (status != CaseStatus.Pending && status != CaseStatus.Deleted)
                throw new BadRequestException("Admin case filter only supports 'Pending' or 'Deleted' statuses.");

            // Deleted cases are sorted by when they were deleted (most recent first)
            // Pending cases are sorted by creation date (oldest first — review queue order)
            var query = status == CaseStatus.Deleted
                ? _unitOfWork.Repository<LongTermMissingCase>()
                    .Query(tracked: false, orderBy: c => c.DeletedAt!, orderByDirection: OrderBy.Descending, includes: c => c.Photos)
                    .Where(c => c.Status == CaseStatus.Deleted)
                : _unitOfWork.Repository<LongTermMissingCase>()
                    .Query(tracked: false, orderBy: c => c.CreatedAt, orderByDirection: OrderBy.Ascending, includes: c => c.Photos)
                    .Where(c => c.Status == CaseStatus.Pending);

            var items = await query.ToListAsync();
            return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(items);
        }

        // ─────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────

        public async Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId)
        {
            var entity = _mapper.Map<LongTermMissingCase>(dto);

            entity.UserId = userId;
            entity.CaseType = CaseType.LongTerm;
            entity.Status = CaseStatus.Pending;
            entity.CreatedAt = DateTime.UtcNow;
            entity.CaseCode = GenerateCaseCode();
            entity.Street ??= string.Empty;
            entity.AgeCategoryId = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);

            if (dto.PoliceReportImage is not null)
                entity.PoliceReportImage = await _fileStorage.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");

            if (dto.Photos is not null)
            {
                foreach (var photo in dto.Photos)
                {
                    var path = await _fileStorage.SaveFileAsync(photo, "long-term");
                    entity.Photos.Add(new CasePhoto { ImagePath = path });
                }
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LongTermMissingCase>().CreateAsync(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Long-term case created. CaseId={CaseId}, CaseCode={CaseCode}, UserId={UserId}",
                    entity.Id, entity.CaseCode, userId);

                return entity.Id;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to create long-term case for UserId={UserId}", userId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────

        public async Task UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                .GetOneAsync(
                    c => c.Id == id && c.Status != CaseStatus.Deleted,
                    tracked: true,
                    c => c.Photos);

            if (entity is null)
            {
                _logger.LogWarning("Update failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning("Unauthorized update on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                throw new ForbiddenException("You are not allowed to update this case.");
            }

            if (dto.Age.HasValue)
            {
                entity.Age = dto.Age.Value;
                entity.AgeCategoryId = await AgeCategoryHelper.ResolveAgeCategoryIdAsync(_unitOfWork, entity.Age);
            }

            if (dto.Gender.HasValue) entity.Gender = dto.Gender.Value;
            if (dto.FName is not null) entity.FName = dto.FName;
            if (dto.SName is not null) entity.SName = dto.SName;
            if (dto.TName is not null) entity.TName = dto.TName;
            if (dto.LName is not null) entity.LName = dto.LName;
            if (dto.Relation.HasValue) entity.Relation = dto.Relation.Value;
            if (dto.Description is not null) entity.Description = dto.Description;
            if (dto.Government is not null) entity.Government = dto.Government;
            if (dto.City is not null) entity.City = dto.City;
            if (dto.Street is not null) entity.Street = dto.Street;

            if (dto.PoliceReportImage is not null)
            {
                if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                    _fileStorage.DeleteFile(entity.PoliceReportImage);

                entity.PoliceReportImage = await _fileStorage.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");
            }

            if (dto.RemovedPhotoIds is { Count: > 0 })
            {
                var toRemove = entity.Photos.Where(p => dto.RemovedPhotoIds.Contains(p.Id)).ToList();
                foreach (var photo in toRemove)
                {
                    _fileStorage.DeleteFile(photo.ImagePath);
                    entity.Photos.Remove(photo);
                    _unitOfWork.Repository<CasePhoto>().Remove(photo);
                }
            }

            if (dto.NewPhotos is not null)
            {
                foreach (var photo in dto.NewPhotos)
                {
                    var path = await _fileStorage.SaveFileAsync(photo, "long-term");
                    entity.Photos.Add(new CasePhoto { ImagePath = path });
                }
            }

            if (!isAdmin && entity.Status != CaseStatus.Pending)
            {
                entity.PreviousStatus = entity.Status;
                entity.Status = CaseStatus.Pending;
            }

            _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Long-term case {CaseId} updated by UserId={UserId} (IsAdmin={IsAdmin})", id, userId, isAdmin);
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE (soft)
        // ─────────────────────────────────────────────────────────────

        public async Task DeleteAsync(long id, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                .GetOneAsync(
                    c => c.Id == id && c.Status != CaseStatus.Deleted,
                    tracked: true);

            if (entity is null)
            {
                _logger.LogWarning("Delete failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning("Unauthorized delete on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                throw new ForbiddenException("You are not allowed to delete this case.");
            }

            entity.PreviousStatus = entity.Status;
            entity.Status = CaseStatus.Deleted;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = userId;

            _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogWarning(
                "LongTermCase {CaseId} soft-deleted (was {PrevStatus}) by UserId={UserId} (IsAdmin={IsAdmin})",
                id, entity.PreviousStatus, userId, isAdmin);
        }

        // ─────────────────────────────────────────────────────────────
        // PERMANENT DELETE — Admin only
        // ─────────────────────────────────────────────────────────────

        public async Task PermanentDeleteAsync(long id)
        {
            var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                .GetOneAsync(
                    c => c.Id == id && c.Status == CaseStatus.Deleted,
                    tracked: true,
                    c => c.Photos,
                    c => c.FoundPersonInfo!);

            if (entity is null)
            {
                _logger.LogWarning("PermanentDelete failed - LongTermCase {CaseId} not found or not soft-deleted.", id);
                throw new NotFoundException(
                    $"Soft-deleted long-term case with id {id} was not found. Only deleted cases can be permanently removed.");
            }

            foreach (var photo in entity.Photos)
                _fileStorage.DeleteFile(photo.ImagePath);

            if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                _fileStorage.DeleteFile(entity.PoliceReportImage);

            _unitOfWork.Repository<LongTermMissingCase>().Remove(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogWarning("LongTermCase {CaseId} permanently deleted by Admin.", id);
        }

        // ─────────────────────────────────────────────────────────────
        // APPROVE / REJECT
        // ─────────────────────────────────────────────────────────────

        public async Task ApproveAsync(long id)
        {
            var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                .GetOneAsync(
                    c => c.Id == id && c.Status == CaseStatus.Pending,
                    tracked: true);

            if (entity is null)
            {
                _logger.LogWarning("Approve failed - LongTermCase {CaseId} not found or not Pending.", id);
                throw new NotFoundException($"Pending long-term case with id {id} was not found.");
            }

            entity.Status = CaseStatus.Active;
            entity.PreviousStatus = null;

            _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("LongTermCase {CaseId} approved (Pending → Active).", id);
        }

        public async Task RejectAsync(long id)
        {
            var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                .GetOneAsync(
                    c => c.Id == id && c.Status == CaseStatus.Pending,
                    tracked: true);

            if (entity is null)
            {
                _logger.LogWarning("Reject failed - LongTermCase {CaseId} not found or not Pending.", id);
                throw new NotFoundException($"Pending long-term case with id {id} was not found.");
            }

            if (entity.PreviousStatus.HasValue)
            {
                entity.Status = entity.PreviousStatus.Value;
                entity.PreviousStatus = null;
            }
            else
            {
                entity.Status = CaseStatus.Rejected;
            }

            _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("LongTermCase {CaseId} rejected (Pending → {Status}).", id, entity.Status);
        }

        // ─────────────────────────────────────────────────────────────
        // MARK AS FOUND
        // ─────────────────────────────────────────────────────────────

        public async Task MarkAsFoundedAsync(long id, MarkAsFoundedDto dto, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.Repository<LongTermMissingCase>()
                .GetOneAsync(
                    c => c.Id == id && c.Status == CaseStatus.Active,
                    tracked: true);

            if (entity is null)
            {
                _logger.LogWarning(
                    "MarkAsFounded failed - LongTermCase {CaseId} not found or not Active. UserId={UserId}", id, userId);
                throw new NotFoundException($"Active long-term case with id {id} was not found.");
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized MarkAsFounded on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                throw new ForbiddenException("You are not allowed to mark this case as found.");
            }

            var foundInfo = new FoundPersonInfo
            {
                CaseId = entity.Id,
                Description = dto.Description,
                Government = dto.Government,
                City = dto.City,
                Street = dto.Street ?? string.Empty,
                FoundedAt = DateOnly.FromDateTime(DateTime.UtcNow),
                FoundedUserId = userId
            };

            entity.Status = CaseStatus.Found;

            await _unitOfWork.Repository<FoundPersonInfo>().CreateAsync(foundInfo);
            _unitOfWork.Repository<LongTermMissingCase>().Update(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("LongTermCase {CaseId} marked as Found by UserId={UserId}.", id, userId);
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────

        private static string GenerateCaseCode() =>
            $"LT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";


    }
}