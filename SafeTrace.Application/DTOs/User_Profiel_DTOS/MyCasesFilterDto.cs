namespace SafeTrace.Application.DTOs.User_Profiel_DTOS;

public class MyCasesFilterDto
{
    public string? FullName { get; set; }
    public string? CaseCode { get; set; }
    public CaseType? CaseType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}