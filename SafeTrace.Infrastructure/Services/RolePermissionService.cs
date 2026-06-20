using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.RolePermission.Request;
using SafeTrace.Application.DTOs.RolePermission.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using System.Reflection;
using System.Security.Claims;

namespace SafeTrace.Infrastructure.Services
{
    public class RolePermissionService : IRolePermissionService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RolePermissionService> _logger;

        public RolePermissionService(RoleManager<IdentityRole> roleManager, ILogger<RolePermissionService> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles.Select(r => new RoleDto
                                                {
                                                    Id = r.Id,
                                                    Name = r.Name!
                                                })
                                                .ToListAsync();

            return ApiResponse<List<RoleDto>>.Ok(roles);
        }

        public async Task<ApiResponse<RolePermissionsResponseDto>> GetPermissionsByRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) throw new NotFoundException("Role not found.");

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var assignedPermissions = existingClaims.Where(c => c.Type == "Permission")
                                                    .Select(c => c.Value)
                                                    .ToList();

            var allPermissions = new List<string>();
            var modules = typeof(Application.Constants.Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
            foreach (var module in modules)
            {
                var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                foreach (var field in fields)
                {
                    var value = field.GetValue(null)?.ToString();
                    if (value != null) allPermissions.Add(value);
                }
            }

            var response = new RolePermissionsResponseDto
            {
                RoleId = role.Id,
                RoleName = role.Name!,
                Permissions = allPermissions.Select(p => new RolePermissionDto
                {
                    PermissionValue = p,
                    IsSelected = assignedPermissions.Contains(p)
                }).ToList()
            };

            return ApiResponse<RolePermissionsResponseDto>.Ok(response);
        }

        public async Task<ApiResponse<string>> UpdateRolePermissionsAsync(UpdateRolePermissionsDto dto)
        {
            var role = await _roleManager.FindByIdAsync(dto.RoleId);
            if (role == null) throw new NotFoundException("Role not found.");

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = claims.Where(c => c.Type == "Permission");
            foreach (var claim in permissionClaims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var permission in dto.SelectedPermissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }

            return ApiResponse<string>.Ok(role.Id, "Role permissions updated successfully.");
        }
    }
}