using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.FacebookPages.Request
{
    public class ReconnectFacebookPageDto
    {
        [Required(ErrorMessage = "رمز الوصول إلى Facebook مطلوب.")]
        public string AccessToken { get; set; } = null!;
    }
}
