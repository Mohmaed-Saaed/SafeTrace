namespace SafeTrace.Application.DTOs.Dashboard.Request
{
    public class AuditLogQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchEmail { get; set; }
        public string? SearchTable { get; set; }
        public string? SearchType { get; set; }
    }
}
