using AutoMapper;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Common.Helpers;
using SafeTrace.Application.Common.Models;
using SafeTrace.Application.DTOs.LongTermCases;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<PagedResultDto<LongTermCaseCardDto>> GetAllAsync(LongTermCaseFilterDto filter)
        {
            var name = filter.Name?.Trim();
            var gender = filter.Gender;

            var filterByAge = filter.AgeCategory.HasValue;
            var ageRange = filterByAge
                ? AgeCategoryHelper.GetRange(filter.AgeCategory!.Value)
                : (Min: 0, Max: 0);
            var ageMin = ageRange.Min;
            var ageMax = ageRange.Max;

            Expression<Func<LongTermMissingCase, bool>> predicate = c =>
                !c.IsDeleted &&
                c.Status == CaseStatus.Active &&
                (string.IsNullOrEmpty(name) ||
                    (c.FName ?? "").Contains(name) ||
                    (c.SName ?? "").Contains(name) ||
                    (c.TName ?? "").Contains(name) ||
                    (c.LName ?? "").Contains(name)) &&
                (gender == null || c.Gender == gender) &&
                (!filterByAge || (c.Age >= ageMin && c.Age <= ageMax));

            var totalCount = await _unitOfWork.LongTermMissingCaseRepository.CountAsync(predicate);

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize < 1 ? 12 : filter.PageSize;

            var orderDirection = filter.SortDescending ? OrderBy.Descending : OrderBy.Ascending;

            var items = await _unitOfWork.LongTermMissingCaseRepository.GetAllAsync(
                predicate,
                false,
                c => c.CreatedAt,
                orderDirection,
                pageNumber,
                pageSize,
                c => c.Photos);

            return new PagedResultDto<LongTermCaseCardDto>
            {
                Items = _mapper.Map<IEnumerable<LongTermCaseCardDto>>(items),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<LongTermCaseDetailsDto> GetByIdAsync(long id, bool includeDeleted = false)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id && (includeDeleted || !c.IsDeleted),
                false,
                c => c.Photos,
                c => c.FoundPersonInfo!,
                c => c.User);

            if (entity == null)
                throw new NotFoundException($"Long-term case with id {id} was not found.");

            return _mapper.Map<LongTermCaseDetailsDto>(entity);
        }

        public async Task<IEnumerable<LongTermCaseCardDto>> GetMyCasesAsync(string userId)
        {
            // Soft-deleted cases are hidden from the owner too - only Admin can see them (GetDeletedCasesAsync)
            var entities = await _unitOfWork.LongTermMissingCaseRepository.GetAllAsync(
                c => c.UserId == userId && !c.IsDeleted,
                false,
                c => c.CreatedAt,
                OrderBy.Descending,
                null,
                null,
                c => c.Photos);

            return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(entities);
        }

        public async Task<IEnumerable<LongTermCaseCardDto>> GetFoundedCasesAsync()
        {
            var entities = await _unitOfWork.LongTermMissingCaseRepository.GetAllAsync(
                c => c.Status == CaseStatus.Found && !c.IsDeleted,
                false,
                c => c.CreatedAt,
                OrderBy.Descending,
                null,
                null,
                c => c.Photos);

            return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(entities);
        }

        public async Task<IEnumerable<LongTermCaseCardDto>> GetPendingCasesAsync()
        {
            var entities = await _unitOfWork.LongTermMissingCaseRepository.GetAllAsync(
                c => c.Status == CaseStatus.Pending && !c.IsDeleted,
                false,
                c => c.CreatedAt,
                OrderBy.Ascending,
                null,
                null,
                c => c.Photos);

            return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(entities);
        }

        public async Task<IEnumerable<LongTermCaseCardDto>> GetDeletedCasesAsync()
        {
            // Admin-only "trash" view: cases users deleted, still here until an Admin restores or purges them
            var entities = await _unitOfWork.LongTermMissingCaseRepository.GetAllAsync(
                c => c.IsDeleted,
                false,
                c => c.DeletedAt!,
                OrderBy.Descending,
                null,
                null,
                c => c.Photos);

            return _mapper.Map<IEnumerable<LongTermCaseCardDto>>(entities);
        }

        public async Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId)
        {
            var entity = _mapper.Map<LongTermMissingCase>(dto);

            entity.UserId = userId;
            entity.CaseType = CaseType.LongTerm;
            entity.Status = CaseStatus.Pending; // FR-20: requires Admin approval before going public
            entity.CreatedAt = DateTime.UtcNow;
            entity.CaseCode = GenerateCaseCode();
            entity.Street ??= string.Empty; // Street is non-nullable on the entity
            entity.IsDeleted = false;

            if (dto.PoliceReportImage != null)
            {
                // Throws BadRequestException on failure - caught by the global exception handler
                entity.PoliceReportImage = await _fileStorage.SaveFileAsync(
                    dto.PoliceReportImage,
                    "long-term/police-reports");
            }

            if (dto.Photos != null)
            {
                foreach (var photo in dto.Photos)
                {
                    var path = await _fileStorage.SaveFileAsync(photo, "long-term");

                    // NOTE: previously the uploaded path was never attached to the entity - fixed here
                    entity.Photos.Add(new CasePhoto { ImagePath = path });
                }
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.LongTermMissingCaseRepository.CreateAsync(entity);
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

        public async Task UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id && !c.IsDeleted, true, c => c.Photos);

            if (entity == null)
            {
                _logger.LogWarning("Update failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized update attempt on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                throw new ForbiddenException("You are not allowed to update this case.");
            }

            if (dto.Gender.HasValue) entity.Gender = dto.Gender.Value;
            if (dto.FName != null) entity.FName = dto.FName;
            if (dto.SName != null) entity.SName = dto.SName;
            if (dto.TName != null) entity.TName = dto.TName;
            if (dto.LName != null) entity.LName = dto.LName;
            if (dto.Age.HasValue) entity.Age = dto.Age.Value;
            if (dto.Relation.HasValue) entity.Relation = dto.Relation.Value;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.Government != null) entity.Government = dto.Government;
            if (dto.City != null) entity.City = dto.City;
            if (dto.Street != null) entity.Street = dto.Street;

            if (dto.PoliceReportImage != null)
            {
                if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                    _fileStorage.DeleteFile(entity.PoliceReportImage);

                entity.PoliceReportImage = await _fileStorage.SaveFileAsync(
                    dto.PoliceReportImage,
                    "long-term/police-reports");
            }

            if (dto.RemovedPhotoIds != null && dto.RemovedPhotoIds.Count > 0)
            {
                var toRemove = entity.Photos.Where(p => dto.RemovedPhotoIds.Contains(p.Id)).ToList();
                foreach (var photo in toRemove)
                {
                    _fileStorage.DeleteFile(photo.ImagePath);
                    entity.Photos.Remove(photo);
                    await _unitOfWork.CasePhotoRepository.DeleteAsync(photo);
                }
            }

            if (dto.NewPhotos != null)
            {
                foreach (var photo in dto.NewPhotos)
                {
                    var path = await _fileStorage.SaveFileAsync(photo, "long-term");

                    // NOTE: previously the uploaded path was never attached to the entity - fixed here
                    entity.Photos.Add(new CasePhoto { ImagePath = path });
                }
            }

            // Edits made by a regular (non-admin) user must be re-approved by an Admin before
            // the case is public again. We remember the status it had before the edit so a
            // Reject only undoes the edit instead of closing an already-active case.
            if (!isAdmin && entity.Status != CaseStatus.Pending)
            {
                entity.PreviousStatus = entity.Status;
                entity.Status = CaseStatus.Pending;
            }

            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} updated by UserId={UserId} (IsAdmin={IsAdmin})", id, userId, isAdmin);
        }

        public async Task DeleteAsync(long id, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id && !c.IsDeleted, true);

            if (entity == null)
            {
                _logger.LogWarning("Delete failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized delete attempt on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                throw new ForbiddenException("You are not allowed to delete this case.");
            }

            // Soft delete only: files and the row stay in place so Admin can restore or purge it
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = userId;

            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogWarning("Long-term case {CaseId} soft-deleted by UserId={UserId} (IsAdmin={IsAdmin})", id, userId, isAdmin);
        }

        public async Task RestoreAsync(long id)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id && c.IsDeleted, true);

            if (entity == null)
            {
                _logger.LogWarning("Restore failed - deleted LongTermCase {CaseId} not found.", id);
                throw new NotFoundException($"Deleted long-term case with id {id} was not found.");
            }

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedByUserId = null;

            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} restored by Admin.", id);
        }

        public async Task PermanentDeleteAsync(long id)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id, true, c => c.Photos, c => c.FoundPersonInfo!);

            if (entity == null)
            {
                _logger.LogWarning("Permanent delete failed - LongTermCase {CaseId} not found.", id);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (!entity.IsDeleted)
            {
                _logger.LogWarning("Permanent delete blocked - LongTermCase {CaseId} is not soft-deleted yet.", id);
                throw new ConflictException("Only soft-deleted cases can be permanently deleted. Delete it first.");
            }

            foreach (var photo in entity.Photos)
                _fileStorage.DeleteFile(photo.ImagePath);

            if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                _fileStorage.DeleteFile(entity.PoliceReportImage);

            await _unitOfWork.LongTermMissingCaseRepository.DeleteAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogWarning("Long-term case {CaseId} permanently deleted by Admin.", id);
        }

        public async Task ApproveAsync(long id)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id && !c.IsDeleted, true);

            if (entity == null)
            {
                _logger.LogWarning("Approve failed - LongTermCase {CaseId} not found.", id);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (entity.Status != CaseStatus.Pending)
            {
                _logger.LogWarning("Approve failed - LongTermCase {CaseId} is not Pending.", id);
                throw new ConflictException("Only pending cases can be approved.");
            }

            entity.Status = CaseStatus.Active;
            entity.PreviousStatus = null;

            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} approved (Pending -> Active).", id);
        }

        public async Task RejectAsync(long id)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id && !c.IsDeleted, true);

            if (entity == null)
            {
                _logger.LogWarning("Reject failed - LongTermCase {CaseId} not found.", id);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (entity.Status != CaseStatus.Pending)
            {
                _logger.LogWarning("Reject failed - LongTermCase {CaseId} is not Pending.", id);
                throw new ConflictException("Only pending cases can be rejected.");
            }

            if (entity.PreviousStatus.HasValue)
            {
                // This Pending state came from a user edit on an already-approved case:
                // rejecting the edit reverts it instead of closing the whole case.
                entity.Status = entity.PreviousStatus.Value;
                entity.PreviousStatus = null;
            }
            else
            {
                // This was a brand-new case awaiting first approval - reject closes it.
                entity.Status = CaseStatus.Closed;
            }

            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} rejected (Pending -> {Status}).", id, entity.Status);
        }

        public async Task MarkAsFoundedAsync(long id, MarkAsFoundedDto dto, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id && !c.IsDeleted, true);

            if (entity == null)
            {
                _logger.LogWarning("MarkAsFounded failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                throw new NotFoundException($"Long-term case with id {id} was not found.");
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized MarkAsFounded attempt on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                throw new ForbiddenException("You are not allowed to mark this case as found.");
            }

            if (entity.Status != CaseStatus.Active)
            {
                _logger.LogWarning("MarkAsFounded failed - LongTermCase {CaseId} is not Active (Status={Status}).", id, entity.Status);
                throw new ConflictException("Only active cases can be marked as found.");
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

            await _unitOfWork.FoundPersonInfoRepository.CreateAsync(foundInfo);
            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} marked as Found by UserId={UserId}.", id, userId);
        }

        private static string GenerateCaseCode()
        {
            return $"LT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }
    }
}
