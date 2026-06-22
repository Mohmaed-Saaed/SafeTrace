using SafeTrace.Application.DTOs.MissingCases.Request;
using SafeTrace.Application.DTOs.UrgentMissingCase;

namespace SafeTrace.Application.Services
{
    internal class UrgentCaseService : IUrgentCaseService
    {
        private readonly ILogger _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UrgentCaseService(ILogger<UrgentCaseService> logger, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ApiResponse<IEnumerable<UrgentCaseListItemDto>>> GetAllAsync(FilterCasesDto filter)
        {

            var items = await _unitOfWork.Repository<UrgentCase>();

            var data = _mapper.Map<IEnumerable<UrgentCaseListItemDto>>(items);

            return new ApiResponse<IEnumerable<UrgentCaseListItemDto>>
            {
                Success = true,
                Data = data,
                Message = "Urgent cases retrieved successfully"
            };
        }           
    }
}
