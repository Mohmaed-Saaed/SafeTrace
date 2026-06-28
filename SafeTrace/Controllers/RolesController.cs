using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.RolePermission.Request;
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

        [HttpGet]
        [HasPermission(Permissions.Roles.GetAll)]
        public async Task<IActionResult> GetAllRoles()
        {
            var response = await _rolePermissionService.GetAllRolesAsync();
            return Ok(response);
        }

        [HttpPost("create")]
        [HasPermission(Permissions.Roles.Create)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            var response = await _rolePermissionService.CreateRoleAsync(dto);
            return Ok(response);
        }

        [HttpDelete("delete/{roleId}")]
        [HasPermission(Permissions.Roles.Delete)]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var response = await _rolePermissionService.DeleteRoleAsync(roleId);
            return Ok(response);
        }

        [HttpGet("GetPermissionsBy/{roleId}")]
        [HasPermission(Permissions.Roles.GetPermissionsByRoleId)]
        public async Task<IActionResult> GetRolePermissions(string roleId)
        {
            var response = await _rolePermissionService.GetPermissionsByRoleAsync(roleId);
            return Ok(response);
        }

        [HttpPost("UpdatePermissions")]
        [HasPermission(Permissions.Roles.UpdateRolePermissions)]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsDto dto)
        {
            var response = await _rolePermissionService.UpdateRolePermissionsAsync(dto);
            return Ok(response);
        }
    }
}