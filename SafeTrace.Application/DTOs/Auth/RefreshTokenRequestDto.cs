using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string ExpiredAccessToken { get; set; } = null!;
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}