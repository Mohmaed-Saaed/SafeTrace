using SafeTrace.Application.DTOs.Cases;
using SafeTrace.Application.Specifications.Cases.Shared;

namespace SafeTrace.Application.Services.CasesServices
{
    public abstract class BaseCaseService<TCase, TDto> where TCase : Case
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;

        protected BaseCaseService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public virtual async Task<PaginationResponseDto<ApiResponse<TDto>>> GetAllAsync(CaseFilterDto filter)
        {
            var spec = new CaseFilterSpecification<TCase>(filter);

            var data = await _unitOfWork.Repository<TCase>().GetAllAsync(spec);

            var mapped = _mapper.Map<List<TDto>>(data);

            var Count = await _unitOfWork.Repository<TCase>().CountAsync(spec);

            return new PaginationResponseDto<ApiResponse<TDto>>
            {
                Items = mapped,
                TotalCount= Count,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
            };
        }
    }
}

