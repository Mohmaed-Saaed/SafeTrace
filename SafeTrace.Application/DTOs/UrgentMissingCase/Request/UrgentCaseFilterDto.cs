using SafeTrace.Application.Common.Enums;

namespace SafeTrace.Application.DTOs.UrgentMissingCase.Request
{
    public class UrgentCaseFilterDto
    {
        public Gender? Gender { get; set; }
        public AgeSort? AgeSort { get; set; }
        public DateSort? DateSort { get; set; }
        public CaseStatus? Status { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public double RadiusInMeters { get; set; } = 50000;
        public string? Government { get; set; }
        public string? City { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
