using SafeTrace.Application.DTOs.CasesMissing.Response;
using SafeTrace.Application.Interfaces.IServices.IMissingCases;

namespace SafeTrace.Application.Services.MissingCases
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

        public Task<IEnumerable<CasesDto>> GetCases()
        {
            throw new NotImplementedException();
        }
    }
}

