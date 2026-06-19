
using NetTopologySuite.Geometries;

namespace SafeTrace.Application.DTOs.UrgentMissingCase{
    public class UrgentCaseFilterDto
    {
        public string? Search { get; set; }
        public Gender? Gender { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public Point? Location { get; set; }
        public SortDirection SortDirection { get; set; } = SortDirection.Newest;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 4;

    }
}
