namespace SafeTrace.Application.DTOs.RolePermission.Request
{
    public class RolePermissionDto
    {
        public string PermissionValue { get; set; } = null!;
        public bool IsSelected { get; set; }
    }
}