using SafeTrace.Application.Common.Validators.Attributes;

namespace SafeTrace.Application.DTOs.Complaints.Request
{
    public class CreateComplaintDto
    {
        [CaseCode]
        public string? CaseCode { get; set; }
        public string Message { get; set; } = null!;
    }
}
