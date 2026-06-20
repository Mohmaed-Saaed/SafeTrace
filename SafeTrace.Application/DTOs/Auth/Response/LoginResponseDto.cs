namespace SafeTrace.Application.DTOs.Auth.Response
{
    public class LoginResponseDto
    {
        public bool RequiresOtp { get; set; }
        public string Email { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}