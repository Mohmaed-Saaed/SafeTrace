using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var response = await _accountService.RegisterAsync(registerDto);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var response = await _accountService.LoginAsync(loginDto);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginDto externalLoginDto)
        {
            var response = await _accountService.GoogleLoginAsync(externalLoginDto);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] ExternalLoginDto externalLoginDto)
        {
            var response = await _accountService.FacebookLoginAsync(externalLoginDto);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string otpCode)
        {
            var response = await _accountService.ConfirmEmailAsync(email, otpCode);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromQuery] string email, [FromQuery] OtpType type)
        {
            var response = await _accountService.ResendOtpAsync(email, type);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromQuery] string email)
        {
            var response = await _accountService.ForgetPasswordAsync(email);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var response = await _accountService.ResetPasswordAsync(resetPasswordDto);
            return Ok(response);
        }

        [HasPermission(Permissions.Account.ChangePassword)]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("تعذر التحقق من هوية المستخدم.");

            var response = await _accountService.ChangePasswordAsync(userId, dto);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var response = await _accountService.RefreshTokenAsync();
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken()
        {
            var response = await _accountService.RevokeTokenAsync();
            return Ok(response);
        }
    }
}