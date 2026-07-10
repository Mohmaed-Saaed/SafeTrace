using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// استرجاع قائمة المستخدمين مع دعم البحث والفلترة.
        /// </summary>
        /// <response code="200">تم جلب بيانات المستخدمين بنجاح.</response>
        /// <response code="401">غير مسجل الدخول.</response>
        /// <response code="403">ليس لديك صلاحية لعرض المستخدمين.</response>
        [ProducesResponseType(typeof(ApiResponse<PaginationResponseDto<GetUserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet]
        [HasPermission(Permissions.Users.GetAll)]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto filterDto)
        {
            var response = await _userService.GetAllUsersAsync(filterDto);
            return Ok(response);
        }

        /// <summary>
        /// استرجاع تفاصيل مستخدم معين بناءً على معرفه (ID).
        /// </summary>
        /// <response code="200">تم جلب تفاصيل المستخدم بنجاح.</response>
        /// <response code="403">ليس لديك صلاحية.</response>
        /// <response code="404">لم يتم العثور على المستخدم.</response>
        [ProducesResponseType(typeof(ApiResponse<GetUserByIdDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpGet("{userId}")]
        [HasPermission(Permissions.Users.GetById)]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var response = await _userService.GetUserByIdAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// إنشاء حساب مستخدم وتعيين دور له من خلال لوحة تحكم الإدارة.
        /// </summary>
        /// <response code="200">تم إنشاء الحساب بنجاح.</response>
        /// <response code="400">بيانات غير صحيحة أو خلل في كلمة المرور.</response>
        /// <response code="403">ليس لديك صلاحية للإنشاء.</response>
        /// <response code="404">الدور (Role) المحدد غير موجود.</response>
        /// <response code="409">البريد الإلكتروني مسجل مسبقاً.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [HttpPost("register-by-admin")]
        [HasPermission(Permissions.Users.RegisterByAdmin)]
        public async Task<IActionResult> RegisterByAdmin([FromBody] RegisterByAdminDto dto)
        {
            var response = await _userService.RegisterByAdminAsync(dto);
            return Ok(response);
        }

        /// <summary>
        /// تغيير دور (Role) مستخدم معين.
        /// </summary>
        /// <remarks>
        /// ملاحظات هامة:
        /// - لا يمكن للمستخدم تغيير دور حسابه الشخصي.
        /// - لا يمكن المساس بدور المالك الأساسي للنظام (Root Admin).
        /// - لا يمكن للمشرف (Moderator) ترقية أو تخفيض صلاحيات مدير (Admin) آخر.
        /// </remarks>
        /// <response code="200">تم تحديث دور المستخدم بنجاح.</response>
        /// <response code="400">فشل في التعيين، أو محاولة تغيير دور الحساب الشخصي.</response>
        /// <response code="403">صلاحيات غير كافية، أو محاولة المساس بصلاحيات مدير النظام.</response>
        /// <response code="404">لم يتم العثور على المستخدم.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("ChangeRole")]
        [HasPermission(Permissions.Users.ChangeRole)]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeUserRoleDto dto)
        {
            var response = await _userService.ChangeUserRoleAsync(GetCurrentUserId(), dto);
            return Ok(response);
        }

        /// <summary>
        /// الموافقة على طلب توثيق هوية المستخدم.
        /// </summary>
        /// <remarks>ينقل المستخدم لدور (VerifiedUser) ويحدث حالته.</remarks>
        /// <response code="200">تمت الموافقة بنجاح.</response>
        /// <response code="400">لا توجد صورة هوية أو حالة المستخدم ليست 'قيد الانتظار'.</response>
        /// <response code="404">المستخدم غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("approve/{userId}")]
        [HasPermission(Permissions.Users.Approve)]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var response = await _userService.ApproveUserAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// رفض طلب توثيق هوية المستخدم.
        /// </summary>
        /// <remarks>يحذف صورة الهوية من الخادم ويعيد حالة المستخدم لغير موثق.</remarks>
        /// <response code="200">تم رفض الطلب بنجاح.</response>
        /// <response code="400">حالة المستخدم ليست 'قيد الانتظار'.</response>
        /// <response code="404">المستخدم غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("reject/{userId}")]
        [HasPermission(Permissions.Users.Reject)]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var response = await _userService.RejectUserAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// تبديل حالة حظر المستخدم (حظر / فك حظر).
        /// </summary>
        /// <remarks>
        /// عند الحظر، يتم إنهاء جميع الجلسات النشطة الخاصة به.
        /// قيود أمنية:
        /// - لا يمكنك حظر حسابك الشخصي.
        /// - المدير الأساسي للنظام محصن ضد الحظر.
        /// - المشرفغير مسموح للمشرف (Moderator) بحظر أو فك حظر مديري النظام (Admins) أو المشرفين الأخرين.
        /// </remarks>
        /// <response code="200">تم تغيير حالة الحظر بنجاح.</response>
        /// <response code="400">محاولة حظر الحساب الشخصي.</response>
        /// <response code="403"> محاولة حظر حساب محصن (المدير الأساسي أو تدخل مشرف ضد مدير أو مشرف أخر).</response>
        /// <response code="404">المستخدم غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("toggle-block/{userId}")]
        [HasPermission(Permissions.Users.ToggleBlock)]
        public async Task<IActionResult> ToggleBlockStatus(string userId)
        {
            var response = await _userService.ToggleUserBlockStatusAsync(GetCurrentUserId(), userId);
            return Ok(response);
        }

        /// <summary>
        /// استرجاع الصلاحيات (Permissions) المعينة لمستخدم بشكل مباشر.
        /// </summary>
        /// <response code="200">تم جلب الصلاحيات المخصصة للمستخدم.</response>
        /// <response code="404">المستخدم غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<UserPermissionsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpGet("GetPermissions/{userId}")]
        [HasPermission(Permissions.Users.GetPermissions)]
        public async Task<IActionResult> GetUserPermissions(string userId)
        {
            var response = await _userService.GetUserPermissionsAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// تعيين صلاحيات مخصصة (Custom Permissions) لمستخدم معين بشكل مباشر (تتجاوز صلاحيات الدور).
        /// </summary>
        /// <response code="200">تم تعيين الصلاحيات المباشرة بنجاح.</response>
        /// <response code="400">فشل في تحديث الصلاحيات.</response>
        /// <response code="404">المستخدم غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("AssignPermissions")]
        [HasPermission(Permissions.Users.AssignPermissions)]
        public async Task<IActionResult> AssignUserPermissions([FromBody] AssignUserPermissionsDto dto)
        {
            var response = await _userService.AssignUserPermissionsAsync(GetCurrentUserId(), dto);
            return Ok(response);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("تعذر التحقق من هوية المستخدم.");
        }
    }
}