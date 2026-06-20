using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.RolePermission.Request;
using SafeTrace.Application.DTOs.RolePermission.Response;namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IRolePermissionService
    {
        Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync();
        Task<ApiResponse<RolePermissionsResponseDto>> GetPermissionsByRoleAsync(string roleId);
        Task<ApiResponse<string>> UpdateRolePermissionsAsync(UpdateRolePermissionsDto dto);
    }
}