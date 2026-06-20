using SafeTrace.Application.DTOs.RolePermission.Request;

namespace SafeTrace.Application.DTOs.RolePermission.Response
{
    public class RolePermissionsResponseDto
    {
        public string RoleId { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public List<RolePermissionDto> Permissions { get; set; } = new();
    }
}