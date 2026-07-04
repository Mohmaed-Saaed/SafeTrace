using SafeTrace.Application.Common.Enums;

namespace SafeTrace.Application.DTOs.Cases.Request
{
    [AgeRangeValid(ErrorMessage = "MaxAge must be greater than or equal to MinAge.")]
    [DateRangeValid(ErrorMessage = "ToDate must be greater than or equal to FromDate.")]
    public abstract class CasesFilterBaseDto
    {
        [EnumDataType(typeof(CaseStatus), ErrorMessage = "Invalid case status value.")]
        public CaseStatus? Status { get; set; }
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value.")]
        public Gender? Gender { get; set; }
 
        // Text Search
        public string? FullName { get; set; } // Will search FName + SName + TName + LName
        [StringLength(100, ErrorMessage = "Government filter cannot exceed 100 characters.")]
        public string? Government { get; set; }
        [StringLength(100, ErrorMessage = "City filter cannot exceed 100 characters.")]
        public string? City { get; set; }
 
        // Age filter
        [Range(0, 120, ErrorMessage = "MinAge must be between 0 and 120.")]
        public int? MinAge { get; set; }
        [Range(0, 120, ErrorMessage = "MaxAge must be between 0 and 120.")]
        public int? MaxAge { get; set; }
 
        // Date filter
        [DataType(DataType.DateTime)]
        public DateTime? FromDate { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? ToDate { get; set; }
 
        // Sorting
        [EnumDataType(typeof(AgeSort), ErrorMessage = "Invalid age sort value.")]
        public AgeSort? AgeSort { get; set; }
        [EnumDataType(typeof(DateSort), ErrorMessage = "Invalid date sort value.")]
        public DateSort? DateSort { get; set; }
 
        // Pagination
        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
        public int Page { get; set; } = 1;
 
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 10;
    }
}