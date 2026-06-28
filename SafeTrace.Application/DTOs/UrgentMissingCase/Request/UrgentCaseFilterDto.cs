using SafeTrace.Application.Common.Enums;

namespace SafeTrace.Application.DTOs.UrgentMissingCase.Request
{
    [AgeRangeValid(ErrorMessage = "MaxAge must be greater than or equal to MinAge.")]
    [DateRangeValid(ErrorMessage = "ToDate must be greater than or equal to FromDate.")]
    public class UrgentCaseFilterDto
    {
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value.")]
        public Gender? Gender { get; set; }

        [EnumDataType(typeof(AgeSort), ErrorMessage = "Invalid age sort value.")]
        public AgeSort? AgeSort { get; set; }

        [EnumDataType(typeof(DateSort), ErrorMessage = "Invalid date sort value.")]
        public DateSort? DateSort { get; set; }

        [EnumDataType(typeof(CaseStatus), ErrorMessage = "Invalid case status value.")]
        public CaseStatus? Status { get; set; }

        [Range(0, 120, ErrorMessage = "MinAge must be between 0 and 120.")]
        public int? MinAge { get; set; }

        [Range(0, 120, ErrorMessage = "MaxAge must be between 0 and 120.")]
        public int? MaxAge { get; set; }

        [Range(100, 500_000, ErrorMessage = "Radius must be between 100 m and 500,000 m.")]
        public double RadiusInMeters { get; set; } = 50_000;

        [StringLength(100, ErrorMessage = "Government filter cannot exceed 100 characters.")]
        public string? Government { get; set; }

        [StringLength(100, ErrorMessage = "City filter cannot exceed 100 characters.")]
        public string? City { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ToDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 10;
    }
}
