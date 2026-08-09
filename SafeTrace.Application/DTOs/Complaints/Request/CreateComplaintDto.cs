using SafeTrace.Application.Common.Validators.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Complaints.Request
{
    public class CreateComplaintDto
    {
        [CaseCode(ErrorMessage = "أدخل كود حالة صحيح (مثال: URG-123)")]
        public string? CaseCode { get; set; }
        [MinLength(20, ErrorMessage = "وصف الشكوى يجب أن يكون 20 حرفاً على الأقل.")]
        [MaxLength(2000, ErrorMessage = "وصف الشكوى يجب ألا يتجاوز 2000 حرف.")]
        public string Message { get; set; } = null!;
    }
}
