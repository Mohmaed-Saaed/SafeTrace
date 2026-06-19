using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.UnKnownDtos;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Common;
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
        //private readonly ILogger<UnKnownCaseService> logger;

        public UnKnownCaseService(IUnitOfWork unitOfWork,IMapper mapper,UserManager<ApplicationUser> userManager ,IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _fileStorageService = fileStorageService;
        }
        public async Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new NotFoundException("User was not found.");

            if (!user.IsVerified)
                throw new UnauthorizedException("You must verify your account before creating a case.");

            var unknownCase = _mapper.Map<UnknownCase>(dto);

            unknownCase.UserId = userId;
            unknownCase.CreatedAt = DateTime.UtcNow;
            unknownCase.Status = CaseStatus.Pending;

            if (dto.Photos != null && dto.Photos.Any())
            {
                foreach (var file in dto.Photos)
                {
                    var result = await _fileStorageService.SaveFileAsync(file, "UnknownCases");

                    if (!result.Success)
                        throw new BadRequestException(result.Message);

                    unknownCase.Photos.Add(new CasePhoto
                    {
                        ImagePath = result.Data!,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                unknownCase.Photos.FirstOrDefault()!.IsPrimary = true;
            }

            await _unitOfWork.UnknownCaseRepository.CreateAsync(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok("Unknown case created successfully");
        }
        public async Task<ApiResponse<string>> ApproveAsync(long id)
        {
            var unknownCase = await _unitOfWork.UnknownCaseRepository
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

            await _unitOfWork.UnknownCaseRepository.UpdateAsync(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "Unknown case approved successfully.");
        }

        public async Task<ApiResponse<IEnumerable<GetUnknownDto>>> GetAllApprovedAsync()
        {
            var unknownCases = await _unitOfWork.UnknownCaseRepository.GetAllAsync(
                x => x.Status == CaseStatus.Active,
                tracked: false,
                includes: x => x.Photos);

            var result = _mapper.Map<IEnumerable<GetUnknownDto>>(unknownCases);

            return ApiResponse<IEnumerable<GetUnknownDto>>.Ok(
                result,
                "Approved unknown cases retrieved successfully.");
        }
        public async Task<ApiResponse<string>> RejectAsync(long id)
        {
            var unknownCase = await _unitOfWork.UnknownCaseRepository
                .GetOneAsync(x => x.Id == id);

            if (unknownCase == null)
                throw new NotFoundException("Unknown case not found.");

            if (unknownCase.Status == CaseStatus.Rejected)
                throw new BadRequestException("This case has already been rejected.");

            if (unknownCase.Status == CaseStatus.Active)
                throw new BadRequestException("Approved cases cannot be rejected.");

            if (unknownCase.Status != CaseStatus.Pending)
                throw new BadRequestException("Only pending cases can be rejected.");

            unknownCase.Status = CaseStatus.Rejected;

            await _unitOfWork.UnknownCaseRepository.UpdateAsync(unknownCase);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(
                message: "Unknown case rejected successfully.");
        }
        public async Task<ApiResponse<IEnumerable<BaseCase>>> GetCasesAsync(UnKnownCaseFilterDto filter)
        {
            var data = await _unitOfWork.BaseCaseRepository.GetAllAsync(
                expression: x =>
                    (string.IsNullOrEmpty(filter.Name) ||
                     (x.FName + " " + x.SName + " " + x.LName).Contains(filter.Name))

                    && (!filter.Gender.HasValue || x.Gender == filter.Gender)

                    && (!filter.AgeCategoryId.HasValue || x.AgeCategoryId == filter.AgeCategoryId),

                orderBy: x => x.CreatedAt,
                orderByDirection: filter.SortDirection == "asc"
                    ? OrderBy.Ascending
                    : OrderBy.Descending
            );

            return ApiResponse<IEnumerable<BaseCase>>.Ok(data);
        }
    }
}
