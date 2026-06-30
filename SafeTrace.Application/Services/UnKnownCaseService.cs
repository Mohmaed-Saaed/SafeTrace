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

            if (user.VerificationStatus != VerificationStatus.Verified)
            {
                logger.LogWarning("User is not verified. UserId: {UserId}", userId);
                throw new UnauthorizedException("You must verify your account before creating a case.");
            }

            logger.LogInformation("User validated successfully. UserId: {UserId}", userId);

            var unknownCase = _mapper.Map<UnknownCase>(dto);

            unknownCase.AgeCategoryId =
             await AgeCategoryHelper.ResolveAgeCategoryIdAsync(
             _unitOfWork,
              dto.Age);

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

            return ApiResponse<string>.Ok(message:"تم إنشاء حالة مجهول الهوية بنجاح");
        }

        public async Task<ApiResponse<string>> ApproveAsync(long id)
        {
            var unknownCase = await _unitOfWork.Repository<UnknownCase>()
                .GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
                throw new NotFoundException("حاله المجهول غبر موجوده.");

            if (unknownCase.Status == CaseStatus.Active)
                throw new BadRequestException("الحاله متوافق عليها مسبقا.");

            if (unknownCase.Status == CaseStatus.Rejected)
                throw new BadRequestException("الحالات المرفوضه لا يتم الموافقه عليها.");

            if (unknownCase.Status != CaseStatus.Pending)
                throw new BadRequestException("فقط حالات قيد الانتظار التى يتم الموافقه عليها");

            unknownCase.Status = CaseStatus.Active;

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "تمت الموافقة على حالة مجهول الهوية بالنشر بنجاح");
        }

        public async Task<ApiResponse<PaginationResponseDto<GetUnknownDto>>>
    GetAllApprovedAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = _unitOfWork
                .Repository<UnknownCase>()
                .Query(
                    tracked: false,
                    includes: x => x.Photos)
                .Where(x =>
                    x.Status == CaseStatus.Active &&
                    x.CaseType == CaseType.Unknown);

            var totalCount = await query.CountAsync();

            var unknownCases = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<GetUnknownDto>>(unknownCases);

            var response = new PaginationResponseDto<GetUnknownDto>
            {
                Items = result,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<GetUnknownDto>>
                .Ok(response,
                    "تم استرجاع حالات مجهولي الهوية المعتمدة بنجاح");
        }
        public async Task<ApiResponse<string>> RejectAsync(long id)
        {
            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>().GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
                throw new NotFoundException("حاله المجهول غير موجوده");

            if (unknownCase.Status == CaseStatus.Rejected)
                throw new BadRequestException("الحاله مرفوضه مسبقا ");

            if (unknownCase.Status == CaseStatus.Active)
                throw new BadRequestException("الحاله المتوافق عليها لا ينبغي ان يتم رفضها");

            if (unknownCase.Status != CaseStatus.Pending)
                throw new BadRequestException("فقط حالات قيد الانتظار ما يتم التوافق عليها");

            unknownCase.Status = CaseStatus.Rejected;

            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "تم رفض حالة مجهول الهوية ");
        }
        public async Task<ApiResponse<PaginationResponseDto<GetUnknownDto>>>
            GetCasesAsync(UnknownFilterUsingbyUserDto filter)
        {
            var query = _unitOfWork
                .Repository<UnknownCase>()
                .Query(tracked: false, includes: x => x.Photos)
                .Where(x =>
                    x.CaseType == CaseType.Unknown &&
                    x.Status == CaseStatus.Active);

            // Search By Name
            if (!string.IsNullOrWhiteSpace(filter.FullName))
            {
                var name = filter.FullName.Trim().ToLower();

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

            // Filter By Gender
            if (filter.Gender.HasValue)
            {
                query = query.Where(x =>
                    x.Gender == filter.Gender.Value);
            }

            // // Filter By Age Category
            // if (filter.AgeCategory.HasValue)
            // {
            //     var range = AgeCategoryHelper
            //         .GetRange(filter.AgeCategory.Value);

            //     query = query.Where(x =>
            //         x.Age >= range.Min &&
            //         x.Age <= range.Max);
            // }

            var totalCount = await query.CountAsync();

            var data = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var result = _mapper.Map<List<GetUnknownDto>>(data);

            return ApiResponse<PaginationResponseDto<GetUnknownDto>>
                .Ok(
                    new PaginationResponseDto<GetUnknownDto>
                    {
                        Items = result,
                        TotalCount = totalCount,
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize
                    },
                    "تم استرجاع حالات مجهولي الهوية بنجاح");
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

                throw new NotFoundException("الحاله غير موجوده");
            }

            if (unknownCase.UserId != userId)
            {
                logger.LogWarning(
                    "Unauthorized update attempt. CaseId: {CaseId}, UserId: {UserId}",
                    id, userId);

                throw new UnauthorizedException("انت غير مسموح لك ان تحدث هذه الحاله");
            }

            if (unknownCase.Status != CaseStatus.Pending &&
                unknownCase.Status != CaseStatus.Active)
            {
                logger.LogWarning(
                    "Update rejected because of invalid status. CaseId: {CaseId}, Status: {Status}",
                    id, unknownCase.Status);

                throw new BadRequestException("هذه الحاله لا يتم تحديثها");
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
                    "يجب ان تضع صوره واحده علي الاقل وان تم حذف جميع الصوره يجب استبدال اول صوره علي الاقل ");
            }

            _mapper.Map(dto, unknownCase);
            unknownCase.AgeCategoryId =
             await AgeCategoryHelper.ResolveAgeCategoryIdAsync(
              _unitOfWork,
                 unknownCase.Age);

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

            return ApiResponse<string>.Ok(message:"تم تحديث حالة مجهول الهوية بنجاح");
        }
        public async Task<ApiResponse<GetUnknownDto>> GetDetailsAsync(long id)
        {
            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>()
                .GetOneAsync(
                    x => x.Id == id,
                    includes: x => x.Photos
              
                   );

            if (unknownCase == null)
                throw new NotFoundException("هذه الحاله غير موجوده");

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

                throw new NotFoundException("هذه الحاله غير موجوده");
            }

            if (unknownCase.UserId != userId)
            {
                logger.LogWarning(
                    "Unauthorized delete attempt. CaseId: {CaseId}, UserId: {UserId}",
                    id, userId);

                throw new UnauthorizedException("انت غير مسموح لك ان تحذه هذه الحاله .");
            }

            if (unknownCase.Status != CaseStatus.Pending &&
                unknownCase.Status != CaseStatus.Active)
            {
                logger.LogWarning(
                    "Delete rejected because of invalid status. CaseId: {CaseId}, Status: {Status}",
                    id, unknownCase.Status);

                throw new BadRequestException(
                    "حالات قيد الانتظار والمتوافق عليها فقط ما تحذف");
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
                message: "تم حذف حالة مجهول الهوية بنجاح");
        }

        public async Task<ApiResponse<string>> FoundUnKnownCase(long id, string userId)
        {
            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>()
                .GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
                throw new NotFoundException("هذه الحاله غير موجوده");


            if (unknownCase.UserId != userId)
                throw new UnauthorizedException("غير مسموح لك بتحديث هذه الحاله ");


            if (unknownCase.Status != CaseStatus.Pending &&
                unknownCase.Status != CaseStatus.Active)
            {
                throw new BadRequestException(
                    "فقط حالات قيد الانتظار والمتوافق عليها ما تحدث");
            }


            unknownCase.Status = CaseStatus.Found;


            _unitOfWork.Repository<UnknownCase>().Update(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "تم تحديث هذه الحاله بنجاح لتكون حاله تم العثور عليها");
        }

        public async Task<ApiResponse<PaginationResponseDto<UnKnownCaseFilterDto>>>
            GetAllWithFilteration(UnKnownCaseFilterStatusDto filter)
        {
            var query = _unitOfWork
                .Repository<UnknownCase>()
                .Query(tracked: false,
                includes: x => x.Photos);

            if (filter.Status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == filter.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var cases = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var result = _mapper
                .Map<List<UnKnownCaseFilterDto>>(cases);

            var response = new PaginationResponseDto<UnKnownCaseFilterDto>
            {
                Items = result,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<
                PaginationResponseDto<UnKnownCaseFilterDto>>
                .Ok(response);
        }

        public async Task<ApiResponse<List<GetMyUnknownnCasesDto>>> GetMyCasesAsync(string userId)
        {
            var cases = await _unitOfWork
                .Repository<UnknownCase>()
                .Query(
                    tracked: false,
                    includes: x => x.Photos)
                .Where(x =>
                    x.UserId == userId &&
                    x.Status != CaseStatus.Deleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<GetMyUnknownnCasesDto>>(cases);

            return ApiResponse<List<GetMyUnknownnCasesDto>>
                .Ok(result);
        }

        public async Task<ApiResponse<string>> HardDeleteUnknownCase(long id)
        {
            var unknownCase = await _unitOfWork
                .Repository<UnknownCase>()
                .GetOneAsync(x => x.Id == id, includes: x => x.Photos);

            if (unknownCase == null)
                throw new NotFoundException("هذه الحاله غير موجوده ");


            if (unknownCase.Photos != null && unknownCase.Photos.Any())
            {
                foreach (var photo in unknownCase.Photos)
                {
                    if (!string.IsNullOrEmpty(photo.ImagePath))
                    {
                        _fileStorageService.DeleteFile(photo.ImagePath);
                    }
                }
            }

            _unitOfWork.Repository<UnknownCase>().Remove(unknownCase);

            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok("تم حذف حالة مجهول الهوية بنجاح");
        }

    }
}