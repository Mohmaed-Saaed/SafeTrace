using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.Common.Validators.Attributes;

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
        [StringLength(243, ErrorMessage = "Full name filter cannot exceed 243 characters.")]
        [ArabicText(ErrorMessage = "Full name must contain Arabic letters and spaces only.")]
        public string? FullName { get; set; } // Will search FName + SName + TName + LName

        [CaseCode]
        public string? CaseCode { get; set; }

        [ValidEgyptianGovernorate(ErrorMessage = "Invalid Governorate.")]
        [StringLength(100, ErrorMessage = "Government filter cannot exceed 100 characters.")]
        public string? Government { get; set; }

        [ValidEgyptianCity(nameof(Government), ErrorMessage = "Invalid City for the selected Governorate.")]
        [StringLength(100, ErrorMessage = "City filter cannot exceed 100 characters.")]
        public string? City { get; set; }
 
        // Age filter
        [Range(1, 120, ErrorMessage = "MinAge must be between 1 and 120.")]
        public int? MinAge { get; set; }

        [Range(1, 120, ErrorMessage = "MaxAge must be between 1 and 120.")]
        public int? MaxAge { get; set; }
 
        // Date filter
        [PastDate(ErrorMessage = "From date cannot be in the future.")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [PastDate(ErrorMessage = "To date cannot be in the future.")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }
 
        // Sorting
        [EnumDataType(typeof(AgeSort), ErrorMessage = "Invalid age sort value.")]
        public AgeSort? AgeSort { get; set; }

        [EnumDataType(typeof(DateSort), ErrorMessage = "Invalid date sort value.")]
        public DateSort? DateSort { get; set; }
 
        // Pagination
        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
        public int Page { get; set; } = 1;

        [Range(1, int.MaxValue, ErrorMessage = "Page size must be at least 1.")]
        public int PageSize { get; set;} = 12;
    }
}
