namespace SafeTrace.Application.DTOs.Complaints.Request
{
    public class CreateComplaintDto
    {
        public string? CaseCode { get; set; }
        public string Message { get; set; } = null!;
    }
}
