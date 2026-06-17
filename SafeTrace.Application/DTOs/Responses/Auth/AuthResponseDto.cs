namespace SafeTrace.Application.DTOs.Responses.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiration { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool IsVerified { get; set; }
    }
}