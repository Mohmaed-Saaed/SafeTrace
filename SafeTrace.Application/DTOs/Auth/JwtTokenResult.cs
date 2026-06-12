namespace SafeTrace.Application.DTOs.Auth
{
    public class JwtTokenResult
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string JwtId { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
    }
}