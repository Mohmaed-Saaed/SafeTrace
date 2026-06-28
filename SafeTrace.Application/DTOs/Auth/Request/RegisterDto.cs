using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth.Request
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد الاسم الأول عن 100 حرف.")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "اسم العائلة مطلوب.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد اسم العائلة عن 100 حرف.")]
        public string LName { get; set; } = null!;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "رقم الهاتف مطلوب.")]
        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة.")]
        [RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "يرجى إدخال رقم هاتف مصري صحيح.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "يجب أن تتكون كلمة المرور من 8 أحرف على الأقل.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "يجب أن تحتوي كلمة المرور على حرف كبير، وحرف صغير، ورقم، ورمز خاص."
        )]
        public string Password { get; set; } = null!;
    }
}