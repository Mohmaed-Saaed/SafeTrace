using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth.Request
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب.")]
        [MinLength(2, ErrorMessage = "الاسم الأول لا يجب أن يقل عن حرفين.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد الاسم الأول عن 100 حرف.")]
        [RegularExpression(@"^[\u0600-\u06FF]+$", ErrorMessage = "الاسم الأول يجب أن يكون كلمة واحدة باللغة العربية فقط ولا يحتوي على مسافات أو أرقام.")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "اسم العائلة مطلوب.")]
        [MinLength(2, ErrorMessage = "اسم العائلة لا يجب أن يقل عن حرفين.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد اسم العائلة عن 100 حرف.")]
        [RegularExpression(@"^[\u0600-\u06FF]+( [\u0600-\u06FF]+)*$", ErrorMessage = "اسم العائلة يجب أن يحتوي على حروف عربية فقط بمسافة واحدة بين الكلمات وبدون مسافات في البداية أو النهاية.")]
        public string LName { get; set; } = null!;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        public string Email { get; set; } = null!;

        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة.")]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "يرجى إدخال رقم هاتف مصري صحيح.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "يجب أن تتكون كلمة المرور من 8 أحرف على الأقل ولا تزيد عن 50 حرف.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])\S+$",
            ErrorMessage = "يجب أن تحتوي كلمة المرور على حرف كبير، وحرف صغير، ورقم، ورمز خاص، ولا يسمح بوجود مسافات."
        )]
        public string Password { get; set; } = null!;
    }
}