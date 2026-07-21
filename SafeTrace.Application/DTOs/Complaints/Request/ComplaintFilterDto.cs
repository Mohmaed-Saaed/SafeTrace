namespace SafeTrace.Application.DTOs.Complaints.Request
{
    public class ComplaintFilterDto
    {
        public string? CaseCode { get; set; }
        public string? Search { get; set; }
        public ComplaintStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}