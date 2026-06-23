namespace SafeTrace.Application.DTOs.RolePermission.Request
{
    public class UpdateRolePermissionsDto
    {
        public string RoleId { get; set; } = null!;
        public List<string> SelectedPermissions { get; set; } = new();
    }
}