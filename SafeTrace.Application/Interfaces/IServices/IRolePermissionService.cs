using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.RolePermission.Request;
using SafeTrace.Application.DTOs.RolePermission.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IRolePermissionService
    {
        Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync();
        Task<ApiResponse<string>> CreateRoleAsync(CreateRoleDto dto);
        Task<ApiResponse<string>> DeleteRoleAsync(string roleId);
        Task<ApiResponse<RolePermissionsResponseDto>> GetPermissionsByRoleAsync(string roleId);
        Task<ApiResponse<string>> UpdateRolePermissionsAsync(UpdateRolePermissionsDto dto);
    }
}