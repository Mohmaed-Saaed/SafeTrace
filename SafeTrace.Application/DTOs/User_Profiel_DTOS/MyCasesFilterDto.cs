using SafeTrace.Application.Common.Validators.Attributes;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS;

public class MyCasesFilterDto
{
    public string? FullName { get; set; }
    [CaseCode]
    public string? CaseCode { get; set; }
    public CaseType? CaseType { get; set; }
    public CaseStatus? Status{ get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}