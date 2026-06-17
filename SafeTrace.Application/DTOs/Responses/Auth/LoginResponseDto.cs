namespace SafeTrace.Application.DTOs.Responses.Auth
{
    public class LoginResponseDto
    {
        public bool RequiresOtp { get; set; }
        public string Email { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}