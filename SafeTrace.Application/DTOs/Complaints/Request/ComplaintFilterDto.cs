using SafeTrace.Application.Common.Validators.Attributes;

namespace SafeTrace.Application.DTOs.Complaints.Request
{
    public class ComplaintFilterDto
    {
        [CaseCode(ErrorMessage = "أدخل كود حالة صحيح (مثال: URG-123)")]
        public string? CaseCode { get; set; }
        public string? ContactType { get; set; }
        public string? Search { get; set; }
        public ComplaintStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}