using SafeTrace.Application.DTOs.Auth;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.Responses.Auth;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IAccountService
    {
        Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto);
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto loginDto);
        Task<ApiResponse<AuthResponseDto>> VerifyLoginOtpAsync(VerifyLoginDto verifyLoginDto);
        Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(ExternalLoginDto externalLoginDto);
        Task<ApiResponse<AuthResponseDto>> FacebookLoginAsync(ExternalLoginDto externalLoginDto);
        Task<ApiResponse<string>> ConfirmEmailAsync(string email, string otpCode);
        Task<ApiResponse<string>> ResendOtpAsync(string email, OtpType type);
        Task<ApiResponse<string>> ForgetPasswordAsync(string email);
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto requestDto);
        Task<ApiResponse<string>> RevokeTokenAsync(string token);
    }
}