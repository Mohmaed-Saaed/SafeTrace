namespace SafeTrace.Application.DTOs.User.Response
{
    public class UserPermissionsResponseDto
    {
        public string UserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public List<UserPermissionDto> Permissions { get; set; } = new();
    }
}