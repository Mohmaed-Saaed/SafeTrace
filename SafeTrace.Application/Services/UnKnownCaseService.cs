using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
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
        //private readonly ILogger<UnKnownCaseService> logger;

        public UnKnownCaseService(IUnitOfWork unitOfWork,IMapper mapper,UserManager<ApplicationUser> userManager ,IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _fileStorageService = fileStorageService;
        }
        public async Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto)//, string userId
        {
            //var user = await _userManager.FindByIdAsync(userId);

            //if (user == null)
            //    throw new NotFoundException($"User was not found.");

            //if (!user.IsVerified)
            //    throw new UnauthorizedException("You must verify your account before creating a case.");

            var unknownCase = _mapper.Map<UnknownCase>(dto);

            //unknownCase.UserId = userId;
            unknownCase.CreatedAt = DateTime.UtcNow;
            unknownCase.Status = CaseStatus.Pending;

  

            if (dto.Photos?.Any() == true)
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

                unknownCase.Photos.First().IsPrimary = true;
            }
            await _unitOfWork.UnknownCaseRepository.CreateAsync(unknownCase);
            await _unitOfWork.SaveAsync();
            return ApiResponse<string>.Ok("Unknown case created successfully");
        }
    }
}
