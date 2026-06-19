using NetTopologySuite;
using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Domain.Common;

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

        public async Task<ApiResponse<IEnumerable<UrgentCaseListItemDto>>> GetAllAsync(UrgentCaseFilterDto filter)
        {
            bool hasAdvancedFilters = !string.IsNullOrWhiteSpace(filter.Search) ||
                filter.Gender.HasValue ||
                filter.MinAge.HasValue ||
                filter.MaxAge.HasValue;

            // Nearby Only
            if (
                filter.Location.Coordinate.CoordinateValue.IsValid &&
                !hasAdvancedFilters
            )
            {
                var nearestCases = await _unitOfWork.UrgentCaseRepository.GetNearestAsync(0.3, 0.2);

                var nearestDto = _mapper.Map<IEnumerable<UrgentCaseListItemDto>>(nearestCases);

                return ApiResponse<IEnumerable<UrgentCaseListItemDto>>.Ok(nearestDto, "Nearest urgent cases retrieved successfully");
            }

            // Search + Filters
            var data = await _unitOfWork.UrgentCaseRepository.GetAllAsync(
                expression:
                    c => (string.IsNullOrWhiteSpace(filter.Search) ||
                            (c.FName ?? "").Contains(filter.Search) ||
                            (c.SName ?? "").Contains(filter.Search))
                        &&
                        (!filter.Gender.HasValue ||
                            c.Gender == filter.Gender)
                        &&
                        (!filter.MinAge.HasValue ||
                            c.Age >= filter.MinAge)
                        &&
                        (!filter.MaxAge.HasValue ||
                            c.Age <= filter.MaxAge),

                orderBy: c => c.CreatedAt,

                orderByDirection:
                    filter.SortDirection == SortDirection.Newest
                        ? OrderBy.Descending
                        : OrderBy.Ascending,

                page: filter.PageNumber,
                pageSize: filter.PageSize,

                includes: c => c.Photos,

                tracked: false
            );

            var dataDto = _mapper.Map<IEnumerable<UrgentCaseListItemDto>>(data);

            return ApiResponse<IEnumerable<UrgentCaseListItemDto>>.Ok(dataDto, "Urgent cases retrieved successfully");
        } 

        public async Task<ApiResponse<UrgentCaseDetailWithRelatedDto>> GetByIdAsync(long id)
        {
            var currentCase = await _unitOfWork.UrgentCaseRepository.GetOneAsync(
                expression: c => c.Id == id,
                includes: [c => c.AgeCategory, c => c.Photos],
                tracked: false
            );

            if (currentCase is null)
                return ApiResponse<UrgentCaseDetailWithRelatedDto>.Fail("Case not found");

            var otherCases = await _unitOfWork.UrgentCaseRepository.GetAllAsync(
                expression: c =>
                    c.Id != id,
                tracked: false
            );

            var relatedCases = otherCases
                .Select(c => new
                {
                    Case = c,
                })
                .OrderBy(x => x)
                .Take(5)
                .Select(x => x.Case)
                .ToList();

            var dataDetailDto = _mapper.Map<UrgentCaseDetailDto>(currentCase);
            var dataRelatedDto = _mapper.Map<List<RelatedUrgentCaseDto>>(relatedCases);

            var dataDto = new UrgentCaseDetailWithRelatedDto() {
                UrgentCaseDetail = dataDetailDto,
                RelatedUrgentCaseDto = dataRelatedDto
            };

            return ApiResponse<UrgentCaseDetailWithRelatedDto>.Ok(
                dataDto,
                "Urgent case retrieved successfully"
            );
        }

        // public async Task CreateAsync(CreateUrgentCaseDto dto)
        // {
            
        //     var entity = _mapper.Map<UrgentCase>(dto);

        //     var factory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);

        //     entity.Location = factory.CreatePoint(new Coordinate( dto.LocationLongitude, dto.LocationLatitude));

        //     await _unitOfWork.UrgentCaseRepository.CreateAsync(entity);

        //     await _unitOfWork.SaveAsync();
        // }
                    
    }
}
