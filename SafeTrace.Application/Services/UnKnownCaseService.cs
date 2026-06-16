using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.DTOs;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;
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
                return ApiResponse<string>.Fail("User not found", 404);

            if (!user.IsVerified)
                return ApiResponse<string>.Fail("User not verified", 401);

            var unknownCase = _mapper.Map<UnknownCase>(dto);

            unknownCase.UserId = userId;
            unknownCase.CreatedAt = DateTime.UtcNow;
            unknownCase.Status = CaseStatus.Pending;

            await _unitOfWork.UnknownCaseRepository.CreateAsync(unknownCase);

            if (dto.Photos?.Any() == true)
            {
                foreach (var file in dto.Photos)
                {
                    var result = await _fileStorageService.SaveFileAsync(file, "UnknownCases");

                    if (!result.Success)
                        return ApiResponse<string>.Fail(result.Message, result.StatusCode);

                    unknownCase.Photos.Add(new CasePhoto
                    {
                        ImagePath = result.Data!,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                unknownCase.Photos.First().IsPrimary = true;
            }

            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok("Unknown case created successfully");
        }
    }
}
