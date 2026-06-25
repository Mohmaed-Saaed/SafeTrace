
using Microsoft.EntityFrameworkCore;
using SafeTrace.Application.DTOs.CasesMissing.Response;

namespace SafeTrace.Application.Services
{
    public class CasesService : ICasesService
    {
        private readonly ILogger<ICasesService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CasesService(ILogger<CasesService> logger, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CasesDto>> GetCases()
        {
            var data = await _unitOfWork.Repository<Case>().Query().ToListAsync();

            var dto = _mapper.Map<IEnumerable<CasesDto>>(data);

            return dto;
        }

        
    }
}

