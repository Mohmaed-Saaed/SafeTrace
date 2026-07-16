using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth.Request
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة.")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "يجب أن تتكون كلمة المرور من 8 أحرف على الأقل.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "يجب أن تحتوي كلمة المرور على حرف كبير، وحرف صغير، ورقم، ورمز خاص."
        )]
        public string NewPassword { get; set; } = null!;
    }
}