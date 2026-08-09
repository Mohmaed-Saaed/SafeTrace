using SafeTrace.Application.Common.Validators.Attributes;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS;

public class MyCasesFilterDto
{
    [StringLength(243, ErrorMessage = "Full name filter cannot exceed 243 characters.")]
    [ArabicText(ErrorMessage = "Full name must contain Arabic letters and spaces only.")]
    public string? FullName { get; set; }

    [CaseCode]
    public string? CaseCode { get; set; }

    [EnumDataType(typeof(CaseType), ErrorMessage = "Invalid case type value.")]
    public CaseType? CaseType { get; set; }

    [EnumDataType(typeof(CaseStatus), ErrorMessage = "Invalid case status value.")]
    public CaseStatus? Status{ get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "Page size must be at least 1.")]
    public int PageSize { get; set; } = 12;
}