using SafeTrace.Domain.Enums;
using System.Collections.Generic;

namespace SafeTrace.Application.DTOs.Auth.Response
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime RefreshTokenExpiration { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? ProfileImage { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public List<string> Permissions { get; set; } = new();
    }
}