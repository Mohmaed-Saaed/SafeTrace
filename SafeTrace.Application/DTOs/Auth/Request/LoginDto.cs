using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth.Request
{
    public class LoginDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        public string Password { get; set; } = null!;
    }
}