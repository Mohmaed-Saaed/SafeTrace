namespace SafeTrace.Application.DTOs.User.Response
{
    public class UserPermissionDto
    {
        public string PermissionValue { get; set; } = null!;
        public bool IsSelected { get; set; }
    }
}