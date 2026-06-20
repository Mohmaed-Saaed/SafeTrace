using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;

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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var response = await _accountService.RegisterAsync(registerDto);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var response = await _accountService.LoginAsync(loginDto);
            return Ok(response);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginDto externalLoginDto)
        {
            var response = await _accountService.GoogleLoginAsync(externalLoginDto);
            return Ok(response);
        }

        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] ExternalLoginDto externalLoginDto)
        {
            var response = await _accountService.FacebookLoginAsync(externalLoginDto);
            return Ok(response);
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string otpCode)
        {
            var response = await _accountService.ConfirmEmailAsync(email, otpCode);
            return Ok(response);
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromQuery] string email, [FromQuery] OtpType type)
        {
            var response = await _accountService.ResendOtpAsync(email, type);
            return Ok(response);
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromQuery] string email)
        {
            var response = await _accountService.ForgetPasswordAsync(email);
            return Ok(response);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var response = await _accountService.ResetPasswordAsync(resetPasswordDto);
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto requestDto)
        {
            var response = await _accountService.RefreshTokenAsync(requestDto);
            return Ok(response);
        }

        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromQuery] string token)
        {
            var response = await _accountService.RevokeTokenAsync(token);
            return Ok(response);
        }
    }
}