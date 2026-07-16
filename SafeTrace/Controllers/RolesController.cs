using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.RolePermission.Request;
using SafeTrace.Application.DTOs.RolePermission.Response;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRolePermissionService _rolePermissionService;

        public RolesController(IRolePermissionService rolePermissionService)
        {
            _rolePermissionService = rolePermissionService;
        }

        /// <summary>
        /// استرجاع جميع الأدوار (Roles) في النظام.
        /// </summary>
        /// <response code="200">تم جلب قائمة الأدوار بنجاح.</response>
        /// <response code="401">المستخدم غير مسجل الدخول.</response>
        /// <response code="403">ليس لديك صلاحية لعرض الأدوار.</response>
        [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet]
        [HasPermission(Permissions.Roles.GetAll)]
        public async Task<IActionResult> GetAllRoles()
        {
            var response = await _rolePermissionService.GetAllRolesAsync();
            return Ok(response);
        }

        /// <summary>
        /// إنشاء دور (Role) جديد في النظام.
        /// </summary>
        /// <response code="200">تم إنشاء الدور بنجاح.</response>
        /// <response code="400">حدث خطأ أثناء الإنشاء.</response>
        /// <response code="403">ليس لديك صلاحية لإضافة دور.</response>
        /// <response code="409">اسم الدور موجود بالفعل.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [HttpPost("create")]
        [HasPermission(Permissions.Roles.Create)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            var response = await _rolePermissionService.CreateRoleAsync(dto);
            return Ok(response);
        }

        /// <summary>
        /// حذف دور (Role) من النظام.
        /// </summary>
        /// <remarks>لا يمكن حذف الأدوار الأساسية (Admin, User, VerifiedUser, Moderator) أو الأدوار المرتبطة بمستخدمين.</remarks>
        /// <response code="200">تم حذف الدور بنجاح.</response>
        /// <response code="400">خطأ في عملية الحذف.</response>
        /// <response code="403">صلاحيات غير كافية أو محاولة حذف دور أساسي.</response>
        /// <response code="404">الدور غير موجود.</response>
        /// <response code="409">لا يمكن الحذف لوجود مستخدمين مرتبطين بهذا الدور.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [HttpDelete("delete/{roleId}")]
        [HasPermission(Permissions.Roles.Delete)]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var response = await _rolePermissionService.DeleteRoleAsync(roleId);
            return Ok(response);
        }

        /// <summary>
        /// استرجاع الصلاحيات (Permissions) المرتبطة بدور معين.
        /// </summary>
        /// <response code="200">تم جلب الصلاحيات الخاصة بالدور.</response>
        /// <response code="403">ليس لديك صلاحية للاطلاع.</response>
        /// <response code="404">الدور المطلوب غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<RolePermissionsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpGet("GetPermissionsBy/{roleId}")]
        [HasPermission(Permissions.Roles.GetPermissionsByRoleId)]
        public async Task<IActionResult> GetRolePermissions(string roleId)
        {
            var response = await _rolePermissionService.GetPermissionsByRoleAsync(roleId);
            return Ok(response);
        }

        /// <summary>
        /// تحديث صلاحيات دور (Role) معين.
        /// </summary>
        /// <remarks>
        /// كإجراء أمني، لا يمكن سحب أو تعديل صلاحيات الدور الإداري الأعلى (Admin) من خلال هذه الواجهة.
        /// </remarks>
        /// <response code="200">تم تحديث الصلاحيات بنجاح.</response>
        /// <response code="403">محاولة تعديل صلاحيات دور الـ (Admin)، أو ليس لديك صلاحية للوصول.</response>
        /// <response code="404">الدور المطلوب غير موجود.</response>
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("UpdatePermissions")]
        [HasPermission(Permissions.Roles.UpdateRolePermissions)]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsDto dto)
        {
            var response = await _rolePermissionService.UpdateRolePermissionsAsync(dto);
            return Ok(response);
        }
    }
}