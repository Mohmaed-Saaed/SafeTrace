using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.Complaints
{
    public class ComplaintFilterDto
    {
        public string? CaseCode { get; set; }
        public ComplaintStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}