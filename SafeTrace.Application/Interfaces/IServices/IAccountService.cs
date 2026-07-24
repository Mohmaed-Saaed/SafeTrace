using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.DTOs.Auth.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IAccountService
    {
        Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
        Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(ExternalLoginDto externalLoginDto);

        Task<ApiResponse<string>> ConfirmEmailAsync(string email, string otpCode);
        Task<ApiResponse<string>> ResendOtpAsync(string email, OtpType type);
        Task<ApiResponse<string>> ForgetPasswordAsync(string email);
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync();
        Task<ApiResponse<string>> RevokeTokenAsync();
    }
}