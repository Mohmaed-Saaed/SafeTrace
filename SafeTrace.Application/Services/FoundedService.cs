using SafeTrace.Application.DTOs.Founded.Request;
using SafeTrace.Application.DTOs.Founded.Response;
using SafeTrace.Application.DTOs.FoundedDTO.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces;


namespace SafeTrace.Infrastructure.Service.Founded
{
    public class FoundedService : IFoundedService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<FoundedService> _logger;
        public FoundedService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<FoundedService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<PaginationResponseDto<FoundPersonListItemDto>> GetAllAsync(FoundedHeaderQueryDTO query)
        {
            query.Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

            var queryable = _unitOfWork.Repository<FoundPersonInfo>()
                .Query(
                    tracked: false,
                    includes:
                    [f => f.Case,f => f.Case.CaseFiles,f => f.Case.AgeCategory])
                .Where(f =>
                    (string.IsNullOrEmpty(query.Search)
                        || f.Case.FName!.Contains(query.Search)
                        || f.Case.SName!.Contains(query.Search))
                        && (!query.CaseType.HasValue || f.Case.CaseType == query.CaseType.Value) 
                    && (!query.Gender.HasValue || f.Case.Gender == query.Gender.Value)
                    && (query.AgeCategory == 0 || f.Case.AgeCategory.Id == query.AgeCategory));

            var totalCount = await queryable.CountAsync();

            var foundedPersons = await queryable
                .OrderByDescending(f => f.Case.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var items = _mapper.Map<List<FoundPersonListItemDto>>(foundedPersons);

            return new PaginationResponseDto<FoundPersonListItemDto>
            {
                Items = items,
                PageNumber = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }
        public async Task<ApiResponse<PostDetailsResponseDTO>> GetDetailsAsync(long id)
        {
            var foundPerson = await _unitOfWork.Repository<FoundPersonInfo>()
                .Query(
                    tracked: false,
                    includes:[
                    f => f.Case,
                    f => f.Case.CaseFiles])
                .FirstOrDefaultAsync(f => f.Id == id);

            if (foundPerson is null)
            {
                _logger.LogWarning($"Found person with Id {id} was not found");
                throw new NotFoundException("Found person is not found");
            }

            return new ApiResponse<PostDetailsResponseDTO>
            {
                Success = true,
                Message = "Post details retrieved successfully",
                Data = _mapper.Map<PostDetailsResponseDTO>(foundPerson)
            };
        }
    }
}