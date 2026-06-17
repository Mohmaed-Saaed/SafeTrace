using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth
{
    public class ExternalLoginDto
    {
        [Required(ErrorMessage = "Provider token is required.")]
        public string ProviderToken { get; set; } = null!;
    }
}