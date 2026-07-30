using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class RejectUserDto
    {
        [Required(ErrorMessage = "يجب كتابة سبب الرفض.")]
        public string Reason { get; set; } = string.Empty;
    }
}
