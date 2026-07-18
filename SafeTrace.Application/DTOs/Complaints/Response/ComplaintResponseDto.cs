namespace SafeTrace.Application.DTOs.Complaints.Response
{
    public class ComplaintResponseDto
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? CaseCode { get; set; }
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}