using System.ComponentModel.DataAnnotations;
using SafeTrace.Application.Common.Enums;

namespace SafeTrace.Application.DTOs.UrgentMissingCase.Request
{
    public class UrgentCaseFilterDto
    {
        [Required]
        public string UserId { get; set; } = null!;
        public Gender? Gender { get; set; }
        public AgeSort? AgeSort { get; set; }
        public DateSort? DateSort { get; set; }
        public CaseStatus? Status { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusInMeters { get; set; }
        public string? Government { get; set; }
        public string? City { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
