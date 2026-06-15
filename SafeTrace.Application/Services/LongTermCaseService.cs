using AutoMapper;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Common.Helpers;
using SafeTrace.Application.Common.Models;
using SafeTrace.Application.DTOs.LongTermCases;
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
    /// <summary>
    /// Business logic for the Long-Term Missing Cases module (FR-18 .. FR-25, FR-47 .. FR-50).
    /// Relies entirely on the existing IUnitOfWork / IRepository&lt;T&gt; abstractions - no direct DbContext access.
    ///
    /// NOTE: ordering uses SafeTrace.Domain.Common.OrderBy.Ascending / OrderBy.Descending (string constants),
    /// as required by the team's IRepository&lt;T&gt;.GetAllAsync signature. If your constants have different
    /// names, this is the only file that needs updating.
    /// </summary>
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

        public async Task<LongTermCaseDetailsDto?> GetByIdAsync(long id)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id,
                false,
                c => c.Photos,
                c => c.FoundPersonInfo!,
                c => c.User);

            return entity == null ? null : _mapper.Map<LongTermCaseDetailsDto>(entity);
        }

        public async Task<IEnumerable<LongTermCaseCardDto>> GetMyCasesAsync(string userId)
        {
            var entities = await _unitOfWork.LongTermMissingCaseRepository.GetAllAsync(
                c => c.UserId == userId,
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
                c => c.Status == CaseStatus.Found,
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
                c => c.Status == CaseStatus.Pending,
                false,
                c => c.CreatedAt,
                OrderBy.Ascending,
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

            if (dto.PoliceReportImage != null)
                entity.PoliceReportImage = await _fileStorage.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");

            if (dto.Photos != null)
            {
                foreach (var photo in dto.Photos)
                {
                    var path = await _fileStorage.SaveFileAsync(photo, "long-term/photos");
                    entity.Photos.Add(new CasePhoto
                    {
                        ImagePath = path,
                        CreatedAt = DateTime.UtcNow
                    });
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

        public async Task<bool> UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(c => c.Id == id, true, c => c.Photos);

            if (entity == null)
            {
                _logger.LogWarning("Update failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                return false;
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized update attempt on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                return false;
            }

            if (dto.Gender.HasValue) entity.Gender = dto.Gender.Value;
            if (dto.FName != null) entity.FName = dto.FName;
            if (dto.SName != null) entity.SName = dto.SName;
            if (dto.TName != null) entity.TName = dto.TName;
            if (dto.LName != null) entity.LName = dto.LName;
            if (dto.Age.HasValue) entity.Age = dto.Age.Value;
            if (dto.Relation.HasValue) entity.Relation = dto.Relation.Value;
            if (dto.LocationLatitude.HasValue) entity.LocationLatitude = dto.LocationLatitude.Value;
            if (dto.LocationLongitude.HasValue) entity.LocationLongitude = dto.LocationLongitude.Value;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.Government != null) entity.Government = dto.Government;
            if (dto.City != null) entity.City = dto.City;
            if (dto.Street != null) entity.Street = dto.Street;

            if (dto.PoliceReportImage != null)
            {
                if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                    _fileStorage.DeleteFile(entity.PoliceReportImage);

                entity.PoliceReportImage = await _fileStorage.SaveFileAsync(dto.PoliceReportImage, "long-term/police-reports");
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
                    var path = await _fileStorage.SaveFileAsync(photo, "long-term/photos");
                    entity.Photos.Add(new CasePhoto { ImagePath = path, CreatedAt = DateTime.UtcNow });
                }
            }

            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} updated by UserId={UserId} (IsAdmin={IsAdmin})", id, userId, isAdmin);
            return true;
        }

        public async Task<bool> DeleteAsync(long id, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(
                c => c.Id == id, true, c => c.Photos, c => c.FoundPersonInfo!);

            if (entity == null)
            {
                _logger.LogWarning("Delete failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                return false;
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized delete attempt on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                return false;
            }

            foreach (var photo in entity.Photos)
                _fileStorage.DeleteFile(photo.ImagePath);

            if (!string.IsNullOrEmpty(entity.PoliceReportImage))
                _fileStorage.DeleteFile(entity.PoliceReportImage);

            await _unitOfWork.LongTermMissingCaseRepository.DeleteAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogWarning("Long-term case {CaseId} deleted by UserId={UserId} (IsAdmin={IsAdmin})", id, userId, isAdmin);
            return true;
        }

        public async Task<bool> ApproveAsync(long id)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(c => c.Id == id, true);
            if (entity == null || entity.Status != CaseStatus.Pending)
            {
                _logger.LogWarning("Approve failed - LongTermCase {CaseId} not found or not Pending.", id);
                return false;
            }

            entity.Status = CaseStatus.Active;
            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} approved (Pending -> Active).", id);
            return true;
        }

        public async Task<bool> RejectAsync(long id)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(c => c.Id == id, true);
            if (entity == null || entity.Status != CaseStatus.Pending)
            {
                _logger.LogWarning("Reject failed - LongTermCase {CaseId} not found or not Pending.", id);
                return false;
            }

            entity.Status = CaseStatus.Closed;
            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} rejected (Pending -> Closed).", id);
            return true;
        }

        public async Task<bool> MarkAsFoundedAsync(long id, MarkAsFoundedDto dto, string userId, bool isAdmin)
        {
            var entity = await _unitOfWork.LongTermMissingCaseRepository.GetOneAsync(c => c.Id == id, true);
            if (entity == null)
            {
                _logger.LogWarning("MarkAsFounded failed - LongTermCase {CaseId} not found. UserId={UserId}", id, userId);
                return false;
            }

            if (entity.UserId != userId && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized MarkAsFounded attempt on LongTermCase {CaseId} by UserId={UserId}", id, userId);
                return false;
            }

            if (entity.Status != CaseStatus.Active)
            {
                _logger.LogWarning("MarkAsFounded failed - LongTermCase {CaseId} is not Active (Status={Status}).", id, entity.Status);
                return false;
            }

            var foundInfo = new FoundPersonInfo
            {
                CaseId = entity.Id,
                Description = dto.Description,
                Government = dto.Government,
                City = dto.City,
                Street = dto.Street ?? string.Empty,
                FoundedAt = DateTime.UtcNow,
                FoundedUserId = userId
            };

            entity.Status = CaseStatus.Found;

            await _unitOfWork.FoundPersonInfoRepository.CreateAsync(foundInfo);
            await _unitOfWork.LongTermMissingCaseRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Long-term case {CaseId} marked as Found by UserId={UserId}.", id, userId);
            return true;
        }

        private static string GenerateCaseCode()
        {
            return $"LT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }
    }
}
