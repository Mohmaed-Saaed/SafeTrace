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
    public class RolePermissionsController : ControllerBase
    {
        private readonly IRolePermissionService _rolePermissionService;

        public RolePermissionsController(IRolePermissionService rolePermissionService)
        {
            _rolePermissionService = rolePermissionService;
        }

        [HttpGet("roles")]
        [HasPermission(Permissions.RolePermission.GetAllRoles)]
        public async Task<IActionResult> GetAllRoles()
        {
            var response = await _rolePermissionService.GetAllRolesAsync();
            return Ok(response);
        }

        [HttpGet("{roleId}")]
        [HasPermission(Permissions.RolePermission.GetPermissionsByRole)]
        public async Task<IActionResult> GetRolePermissions(string roleId)
        {
            var response = await _rolePermissionService.GetPermissionsByRoleAsync(roleId);
            return Ok(response);
        }

        [HttpPost("update")]
        [HasPermission(Permissions.RolePermission.UpdateRolePermissions)]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsDto dto)
        {
            var response = await _rolePermissionService.UpdateRolePermissionsAsync(dto);
            return Ok(response);
        }
    }
}