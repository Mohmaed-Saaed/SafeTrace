using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.UnKnownDtos;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Helpers;
using SafeTrace.Application.Interfaces.IServices;
using Microsoft.EntityFrameworkCore;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Application.Extensions;

namespace SafeTrace.Application.Services
{
    public class UnKnownCaseService : IUnknownCaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileStorageService _fileStorageService;
       private readonly ILogger<UnKnownCaseService> logger;

        public UnKnownCaseService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager, IFileStorageService fileStorageService, ILogger<UnKnownCaseService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _fileStorageService = fileStorageService;
            this.logger = logger;
        }

        public async Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto, string userId)
        {
            logger.LogInformation("Start creating unknown case for UserId: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                logger.LogWarning("User not found. UserId: {UserId}", userId);
                throw new NotFoundException("User was not found.");
            }

            if (!user.IsVerified)
            {
                logger.LogWarning("User is not verified. UserId: {UserId}", userId);
                throw new UnauthorizedException("You must verify your account before creating a case.");
            }

            logger.LogInformation("User validated successfully. UserId: {UserId}", userId);

            var unknownCase = _mapper.Map<UnknownCase>(dto);

            unknownCase.UserId = userId;
            unknownCase.CreatedAt = DateTime.UtcNow;
            unknownCase.Status = CaseStatus.Pending;
            unknownCase.CaseType = CaseType.Unknown;
            unknownCase.CaseCode = Generators.GenerateCaseCode();

            logger.LogInformation("Unknown case object created in memory. CaseCode: {CaseCode}", unknownCase.CaseCode);

            if (dto.Photos != null && dto.Photos.Any())
            {
                logger.LogInformation("Uploading {Count} photos for CaseCode: {CaseCode}", dto.Photos.Count(), unknownCase.CaseCode);

                foreach (var file in dto.Photos)
                {
                    var imagePath = await _fileStorageService.SaveFileAsync(file, "UnknownCases");

                    unknownCase.Photos.Add(new CasePhoto
                    {
                        ImagePath = imagePath,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                unknownCase.Photos.First().IsPrimary = true;

                logger.LogInformation("Photos uploaded successfully for CaseCode: {CaseCode}", unknownCase.CaseCode);
            }

            await _unitOfWork.Repository<UnknownCase>().CreateAsync(unknownCase);
            await _unitOfWork.SaveAsync();

            logger.LogInformation("Unknown case saved successfully. CaseCode: {CaseCode}", unknownCase.CaseCode);

            return ApiResponse<string>.Ok("Unknown case created successfully");
        }

        public async Task<ApiResponse<string>> ApproveAsync(long id)
        {
            var unknownCase = await _unitOfWork.Repository<UnknownCase>()
                .GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
                throw new NotFoundException("Unknown case not found.");

            if (unknownCase.Status == CaseStatus.Active)
                throw new BadRequestException("This case has already been approved.");

            if (unknownCase.Status == CaseStatus.Rejected)
                throw new BadRequestException("Rejected cases cannot be approved.");

            if (unknownCase.Status != CaseStatus.Pending)
                throw new BadRequestException("Only pending cases can be approved.");

            unknownCase.Status = CaseStatus.Active;

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "Unknown case approved successfully.");
        }

        public async Task<ApiResponse<IEnumerable<GetUnknownDto>>> GetAllApprovedAsync()
        {
            var unknownCases = await _unitOfWork
                .Repository<UnknownCase>()
                .Query(
                    tracked: false,
                    includes: x => x.Photos)
                .Where(x =>
                    x.Status == CaseStatus.Active &&
                    x.CaseType == CaseType.Unknown)
                .ToListAsync();

            var result =
                _mapper.Map<IEnumerable<GetUnknownDto>>(
                    unknownCases);

            return ApiResponse<IEnumerable<GetUnknownDto>>
                .Ok(
                    result,
                    "Approved unknown cases retrieved successfully.");
        }
        public async Task<ApiResponse<string>> RejectAsync(long id)
        {
            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>().GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
                throw new NotFoundException("Unknown case not found.");

            if (unknownCase.Status == CaseStatus.Rejected)
                throw new BadRequestException("This case has already been rejected.");

            if (unknownCase.Status == CaseStatus.Active)
                throw new BadRequestException("Approved cases cannot be rejected.");

            if (unknownCase.Status != CaseStatus.Pending)
                throw new BadRequestException("Only pending cases can be rejected.");

            unknownCase.Status = CaseStatus.Rejected;

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "Unknown case rejected successfully.");
        }
        public async Task<ApiResponse<PagedResponse<GetUnknownDto>>> GetCasesAsync(
            UnKnownCaseFilterDto filter)
        {
            var query = _unitOfWork
                .Repository<UnknownCase>()
                .Query(tracked: false);


            query = query.Where(x =>
                x.CaseType == CaseType.Unknown &&
                x.Status == CaseStatus.Active);


           
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim().ToLower();

                query = query.Where(x =>
                    (
                        (x.FName ?? "") + " " +
                        (x.SName ?? "") + " " +
                        (x.TName ?? "") + " " +
                        (x.LName ?? "")
                    )
                    .ToLower()
                    .Contains(name));
            }


            
            if (filter.Gender.HasValue)
            {
                query = query.Where(x =>
                    x.Gender == filter.Gender);
            }


            
            if (filter.AgeCategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.AgeCategoryId == filter.AgeCategoryId);
            }


            if (filter.SortDirection?.ToLower() == "asc")
            {
                query = query.OrderBy(x => x.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(x => x.CreatedAt);
            }


        
            var totalCount = await query.CountAsync();


            
            var data = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();


            var result = _mapper.Map<List<GetUnknownDto>>(data);


            return ApiResponse<PagedResponse<GetUnknownDto>>
                .Ok(
                    new PagedResponse<GetUnknownDto>
                    {
                        Items = result,
                        TotalCount = totalCount,
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize
                    },
                    "Unknown cases retrieved successfully");
        }
        public async Task<ApiResponse<string>> UpdateUnknownCaseAsync(
            long id,
            UpdateUnkownCaseDto dto,
            string userId)
        {
            logger.LogInformation(
                "Starting update for UnknownCase. CaseId: {CaseId}, UserId: {UserId}",
                id, userId);

            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>()
                .GetOneAsync(
                    x => x.Id == id,
                    includes: x => x.Photos);

            if (unknownCase == null)
            {
                logger.LogWarning(
                    "Unknown case not found. CaseId: {CaseId}",
                    id);

                throw new NotFoundException("Case not found.");
            }

            if (unknownCase.UserId != userId)
            {
                logger.LogWarning(
                    "Unauthorized update attempt. CaseId: {CaseId}, UserId: {UserId}",
                    id, userId);

                throw new UnauthorizedException("You are not allowed to update this case.");
            }

            if (unknownCase.Status != CaseStatus.Pending &&
                unknownCase.Status != CaseStatus.Active)
            {
                logger.LogWarning(
                    "Update rejected because of invalid status. CaseId: {CaseId}, Status: {Status}",
                    id, unknownCase.Status);

                throw new BadRequestException("This case cannot be updated.");
            }

            if (unknownCase.Status == CaseStatus.Active)
            {
                logger.LogInformation(
                    "Case status changed from Active to Pending. CaseId: {CaseId}",
                    id);

                unknownCase.Status = CaseStatus.Pending;
            }

            var currentPhotosCount = unknownCase.Photos.Count;
            var deletedCount = dto.DeletedPhotoIds?.Count ?? 0;
            var addedCount = dto.NewPhotos?.Count ?? 0;

            logger.LogInformation(
                "Photos update for CaseId: {CaseId}. Current: {Current}, ToDelete: {Deleted}, ToAdd: {Added}",
                id, currentPhotosCount, deletedCount, addedCount);

            var expectedCount = currentPhotosCount - deletedCount + addedCount;

            if (expectedCount <= 0)
            {
                logger.LogWarning(
                    "Update rejected because it would leave the case without photos. CaseId: {CaseId}",
                    id);

                throw new BadRequestException(
                    "Case must have at least one photo. You must replace the existing photo if you want to remove it.");
            }

            _mapper.Map(dto, unknownCase);

            if (dto.DeletedPhotoIds != null)
            {
                foreach (var photoId in dto.DeletedPhotoIds)
                {
                    var photo = unknownCase.Photos.FirstOrDefault(x => x.Id == photoId);

                    if (photo != null)
                    {
                        logger.LogInformation(
                            "Deleting photo. PhotoId: {PhotoId}, CaseId: {CaseId}",
                            photoId, id);

                        _fileStorageService.DeleteFile(photo.ImagePath);
                        unknownCase.Photos.Remove(photo);
                    }
                }
            }

            if (dto.NewPhotos != null)
            {
                logger.LogInformation(
                    "Adding {Count} new photos to CaseId: {CaseId}",
                    dto.NewPhotos.Count, id);

                foreach (var file in dto.NewPhotos)
                {
                    var imagePath = await _fileStorageService
                        .SaveFileAsync(file, "UnknownCases");

                    unknownCase.Photos.Add(new CasePhoto
                    {
                        ImagePath = imagePath,
                        CreatedAt = DateTime.UtcNow,
                        IsPrimary = false
                    });
                }
            }

            if (unknownCase.Photos.Any() &&
                !unknownCase.Photos.Any(x => x.IsPrimary))
            {
                logger.LogInformation(
                    "Setting first photo as primary. CaseId: {CaseId}",
                    id);

                unknownCase.Photos.First().IsPrimary = true;
            }

            unknownCase.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            logger.LogInformation(
                "Unknown case updated successfully. CaseId: {CaseId}, UserId: {UserId}",
                id, userId);

            return ApiResponse<string>.Ok("Unknown case updated successfully.");
        }
        public async Task<ApiResponse<GetUnknownDto>> GetDetailsAsync(long id)
        {
            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>()
                .GetOneAsync(
                    x => x.Id == id,
                    includes: x => x.Photos);

            if (unknownCase == null)
                throw new NotFoundException("Case not found.");

            var result = _mapper.Map<GetUnknownDto>(unknownCase);

            return ApiResponse<GetUnknownDto>.Ok(result);
        }
        public async Task<ApiResponse<string>> DeleteUnKnownCase(long id, string userId)
        {
            logger.LogInformation(
                "Starting deletion of unknown case. CaseId: {CaseId}, UserId: {UserId}",
                id, userId);

            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>()
                .GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
            {
                logger.LogWarning(
                    "Unknown case not found. CaseId: {CaseId}",
                    id);

                throw new NotFoundException("Unknown case not found.");
            }

            if (unknownCase.UserId != userId)
            {
                logger.LogWarning(
                    "Unauthorized delete attempt. CaseId: {CaseId}, UserId: {UserId}",
                    id, userId);

                throw new UnauthorizedException("You are not allowed to delete this case.");
            }

            if (unknownCase.Status != CaseStatus.Pending &&
                unknownCase.Status != CaseStatus.Active)
            {
                logger.LogWarning(
                    "Delete rejected because of invalid status. CaseId: {CaseId}, Status: {Status}",
                    id, unknownCase.Status);

                throw new BadRequestException(
                    "Only Pending or Active cases can be deleted.");
            }

            unknownCase.Status = CaseStatus.Deleted;
            unknownCase.DeletedAt = DateTime.UtcNow;
            unknownCase.DeletedByUserId = userId;

            logger.LogInformation(
                "Case marked as deleted. CaseId: {CaseId}, DeletedBy: {UserId}",
                id, userId);

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            logger.LogInformation(
                "Unknown case deleted successfully. CaseId: {CaseId}",
                id);

            return ApiResponse<string>.Ok(
                message: "Unknown case deleted successfully.");
        }

        public async Task<ApiResponse<string>> FoundUnKnownCase(long id, string userId)
        {
            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>()
                .GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
                throw new NotFoundException("Unknown case not found.");


            if (unknownCase.UserId != userId)
                throw new UnauthorizedException("You are not allowed to Update this case .");


            if (unknownCase.Status != CaseStatus.Pending &&
                unknownCase.Status != CaseStatus.Active)
            {
                throw new BadRequestException(
                    "Only Pending or Active cases can be Updated to be found.");
            }


            unknownCase.Status = CaseStatus.Found;
     

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "Unknown case deleted successfully.");
        }
    }
}