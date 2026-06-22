using SafeTrace.Application.Common.Enums;

namespace SafeTrace.Application.DTOs.MissingCases.Request{
    public class FilterCasesDto
    {
        public string? Search { get; set; }
        public Gender? Gender { get; set; }

        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        
        public AgeSort AgeSort { get; set; } = AgeSort.None;

        public DateSort DateSort { get; set; } = DateSort.Newest;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double RadiusKm { get; set; } = 10;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
