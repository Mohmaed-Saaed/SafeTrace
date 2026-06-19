using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Application.Extensions;
using SafeTrace.Domain.Entities;

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
            var query = _unitOfWork.Repository<UrgentCase>().Query();

            // Search
            query = query.WhereIf(
                !string.IsNullOrWhiteSpace(filter.Search),
                x => x.FName.Contains(filter.Search!) || x.LName.Contains(filter.Search!));

            // Gender
            query = query.WhereIf(filter.Gender.HasValue, x => x.Gender == filter.Gender);

            // Age
            query = query.WhereIf(filter.MinAge.HasValue, x => x.Age >= filter.MinAge!.Value);

            query = query.WhereIf(filter.MaxAge.HasValue, x => x.Age <= filter.MaxAge!.Value);

            // Nearby Search
            if (filter.Latitude.HasValue && filter.Longitude.HasValue)
            {
                var point = new Point(filter.Longitude.Value, filter.Latitude.Value)
                {
                    SRID = 4326
                };

                var radiusMeters = filter.RadiusKm * 1000;

                query = query.Where(x => x.Location.Distance(point) <= radiusMeters);
            }

            // Total Count
            var totalCount = await query.CountAsync();

            // Data
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new UrgentCaseListItemDto
                {
                    Id = x.Id,
                    FullName = x.FName + " " + x.LName,
                    Age = x.Age,
                    Gender = x.Gender
                })
                .ToListAsync();

            return new ApiResponse<IEnumerable<UrgentCaseListItemDto>>
            {
                Data = items,
            };
        }
        
        // public async Task<ApiResponse<UrgentCaseDetailWithRelatedDto>> GetByIdAsync(long id)
        // {
        //     var currentCase = await _unitOfWork.UrgentCaseRepository.GetOneAsync(
        //         expression: c => c.Id == id,
        //         includes: [c => c.AgeCategory, c => c.Photos],
        //         tracked: false
        //     );

        //     if (currentCase is null)
        //         return ApiResponse<UrgentCaseDetailWithRelatedDto>.Fail("Case not found");

        //     var otherCases = await _unitOfWork.UrgentCaseRepository.GetAllAsync(
        //         expression: c =>
        //             c.Id != id,
        //         tracked: false
        //     );

        //     var relatedCases = otherCases
        //         .Select(c => new
        //         {
        //             Case = c,
        //         })
        //         .OrderBy(x => x)
        //         .Take(5)
        //         .Select(x => x.Case)
        //         .ToList();

        //     var dataDetailDto = _mapper.Map<UrgentCaseDetailDto>(currentCase);
        //     var dataRelatedDto = _mapper.Map<List<RelatedUrgentCaseDto>>(relatedCases);

        //     var dataDto = new UrgentCaseDetailWithRelatedDto() {
        //         UrgentCaseDetail = dataDetailDto,
        //         RelatedUrgentCaseDto = dataRelatedDto
        //     };

        //     return ApiResponse<UrgentCaseDetailWithRelatedDto>.Ok(
        //         dataDto,
        //         "Urgent case retrieved successfully"
        //     );
        // }

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
