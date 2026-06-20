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
        //private readonly ILogger<UnKnownCaseService> logger;

        public UnKnownCaseService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager, IFileStorageService fileStorageService)
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
            unknownCase.CaseType = CaseType.Unknown;
            unknownCase.CaseCode = Generators.GenerateCaseCode();
                       
            if (dto.Photos != null && dto.Photos.Any())
            {
                foreach (var file in dto.Photos)
                {
                    var imagePath = await _fileStorageService
                        .SaveFileAsync(file, "UnknownCases");

                    unknownCase.Photos.Add(new CasePhoto
                    {
                        ImagePath = imagePath,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                unknownCase.Photos.First().IsPrimary = true;
            }

            await _unitOfWork.Repository<UnknownCase>().CreateAsync(unknownCase);
            await _unitOfWork.SaveAsync();

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
    }
}