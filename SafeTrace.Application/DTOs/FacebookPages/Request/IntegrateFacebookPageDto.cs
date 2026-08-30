using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.FacebookPages.Request
{
    public class IntegrateFacebookPageDto
    {
        [Required(ErrorMessage = "رمز وصول صفحة Facebook مطلوب.")]
        public string AccessToken { get; set; } = null!;

        [Required(ErrorMessage = "البريد الإلكتروني للمستخدم مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        [MaxLength(256, ErrorMessage = "البريد الإلكتروني يجب ألا يتجاوز 256 حرفًا.")]
        public string UserEmail { get; set; } = null!;
    }
}
