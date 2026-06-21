
using NetTopologySuite.Geometries;

namespace SafeTrace.Application.DTOs.UrgentMissingCase{
    public class UrgentCaseFilterDto
    {
        public string? Search { get; set; }
        public Gender? Gender { get; set; }

        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double RadiusKm { get; set; } = 10;

        public SortDirection SortDirection { get; set; } = SortDirection.Newest;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
