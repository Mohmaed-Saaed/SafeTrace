using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.DTOs.Auth.Response;
using SafeTrace.Application.DTOs.Responses;
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

        /// <summary>
        /// إنشاء حساب مستخدم جديد.
        /// </summary>
        /// <remarks>
        /// يقوم بإنشاء حساب جديد، وتعيين دور (User) له، ثم إرسال رمز OTP لتأكيد البريد الإلكتروني.
        /// </remarks>
        /// <param name="registerDto">بيانات التسجيل (البريد، كلمة المرور، الاسم... الخ)</param>
        /// <response code="200">تم إنشاء الحساب بنجاح وتم إرسال بريد التأكيد.</response>
        /// <response code="400">بيانات غير صحيحة أو فشل في تلبية شروط كلمة المرور.</response>
        /// <response code="409">البريد الإلكتروني مسجل مسبقاً.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var response = await _accountService.RegisterAsync(registerDto);
            return Ok(response);
        }

        /// <summary>
        /// تسجيل الدخول للمنصة باستخدام البريد الإلكتروني.
        /// </summary>
        /// <remarks>
        /// يتحقق من بيانات المستخدم ويرجع Access Token ويضع Refresh Token في الـ Cookies.
        /// يجب أن يكون البريد الإلكتروني مؤكداً وغير محظور.
        /// </remarks>
        /// <response code="200">تم تسجيل الدخول بنجاح.</response>
        /// <response code="401">البريد الإلكتروني أو كلمة المرور غير صحيحة.</response>
        /// <response code="403">يرجى تأكيد البريد الإلكتروني أولاً، أو الحساب محظور.</response>
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var response = await _accountService.LoginAsync(loginDto);
            return Ok(response);
        }

        /// <summary>
        /// تسجيل الدخول باستخدام حساب جوجل.
        /// </summary>
        /// <remarks>
        /// يستقبل الـ Provider Token من جوجل ويقوم بإنشاء حساب للمستخدم (إن لم يكن موجوداً) أو تسجيل دخوله.
        /// </remarks>
        /// <response code="200">تم تسجيل الدخول بنجاح.</response>
        /// <response code="400">فشل في استخراج البريد الإلكتروني أو خطأ في عملية التسجيل.</response>
        /// <response code="401">رمز جوجل (Provider Token) غير صالح.</response>
        /// <response code="403">الحساب محظور من قبل الإدارة.</response>
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginDto externalLoginDto)
        {
            var response = await _accountService.GoogleLoginAsync(externalLoginDto);
            return Ok(response);
        }

        /// <summary>
        /// تسجيل الدخول باستخدام حساب فيسبوك.
        /// </summary>
        /// <remarks>
        /// يستقبل الـ Provider Token من فيسبوك للتحقق من هوية المستخدم وتسجيل دخوله.
        /// </remarks>
        /// <response code="200">تم تسجيل الدخول بنجاح.</response>
        /// <response code="400">فشل في استخراج البريد الإلكتروني أو خطأ في عملية التسجيل.</response>
        /// <response code="401">رمز فيسبوك (Provider Token) غير صالح.</response>
        /// <response code="403">الحساب محظور من قبل الإدارة.</response>
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [AllowAnonymous]
        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] ExternalLoginDto externalLoginDto)
        {
            var response = await _accountService.FacebookLoginAsync(externalLoginDto);
            return Ok(response);
        }

        /// <summary>
        /// تأكيد البريد الإلكتروني باستخدام رمز الـ OTP.
        /// </summary>
        /// <response code="200">تم تأكيد البريد الإلكتروني بنجاح.</response>
        /// <response code="400">رمز التحقق غير صحيح أو انتهت صلاحيته.</response>
        /// <response code="404">الحساب غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string otpCode)
        {
            var response = await _accountService.ConfirmEmailAsync(email, otpCode);
            return Ok(response);
        }

        /// <summary>
        /// إعادة إرسال رمز التحقق (OTP) للبريد الإلكتروني.
        /// </summary>
        /// <param name="type">نوع الـ OTP (تأكيد حساب أو استعادة كلمة مرور).</param>
        /// <response code="200">تمت إعادة إرسال رمز التحقق بنجاح.</response>
        /// <response code="404">الحساب غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromQuery] string email, [FromQuery] OtpType type)
        {
            var response = await _accountService.ResendOtpAsync(email, type);
            return Ok(response);
        }

        /// <summary>
        /// طلب إعادة تعيين كلمة المرور (نسيت كلمة المرور).
        /// </summary>
        /// <remarks>يقوم بإرسال رمز OTP للبريد الإلكتروني لتغيير كلمة المرور.</remarks>
        /// <response code="200">تم إرسال الرمز للبريد الإلكتروني.</response>
        /// <response code="403">الحساب محظور.</response>
        /// <response code="404">الحساب غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromQuery] string email)
        {
            var response = await _accountService.ForgetPasswordAsync(email);
            return Ok(response);
        }

        /// <summary>
        /// تعيين كلمة مرور جديدة بعد التحقق من الـ OTP.
        /// </summary>
        /// <remarks>يقوم بتغيير كلمة المرور وتسجيل الخروج من جميع الجلسات النشطة للأمان.</remarks>
        /// <response code="200">تم إعادة تعيين كلمة المرور بنجاح.</response>
        /// <response code="400">رمز التحقق غير صحيح أو كلمة المرور غير مطابقة للشروط.</response>
        /// <response code="403">الحساب محظور.</response>
        /// <response code="404">الحساب غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var response = await _accountService.ResetPasswordAsync(resetPasswordDto);
            return Ok(response);
        }

        /// <summary>
        /// تغيير كلمة المرور الحالية (للمستخدمين المسجلين دخولهم).
        /// </summary>
        /// <response code="200">تم تغيير كلمة المرور بنجاح.</response>
        /// <response code="400">كلمة المرور الحالية غير صحيحة.</response>
        /// <response code="401">المستخدم غير مسجل الدخول.</response>
        /// <response code="404">الحساب غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HasPermission(Permissions.Account.ChangePassword)]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("تعذر التحقق من هوية المستخدم.");

            var response = await _accountService.ChangePasswordAsync(userId, dto);
            return Ok(response);
        }

        /// <summary>
        /// تجديد جلسة المستخدم (Refresh Token).
        /// </summary>
        /// <remarks>
        /// يعتمد على قراءة الـ Refresh Token من ملفات تعريف الارتباط (Cookies) للمتصفح.
        /// ويرجع Access Token جديداً للاستمرار في استخدام المنصة.
        /// </remarks>
        /// <response code="200">تم تجديد الجلسة بنجاح.</response>
        /// <response code="401">الـ Cookie غير موجودة أو الجلسة منتهية، يرجى تسجيل الدخول مجدداً.</response>
        /// <response code="404">صاحب هذه الجلسة غير موجود في النظام.</response>
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var response = await _accountService.RefreshTokenAsync();
            return Ok(response);
        }

        /// <summary>
        /// تسجيل الخروج وإلغاء صلاحية الجلسة.
        /// </summary>
        /// <remarks>يقوم بمسح الـ Refresh Token من قاعدة البيانات ومن الـ Cookies.</remarks>
        /// <response code="200">تم تسجيل الخروج بنجاح.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [AllowAnonymous]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken()
        {
            var response = await _accountService.RevokeTokenAsync();
            return Ok(response);
        }
    }
}