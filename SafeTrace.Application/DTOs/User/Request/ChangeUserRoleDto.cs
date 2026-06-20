using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class ChangeUserRoleDto
    {
        [Required(ErrorMessage = "New role name is required.")]
        public string NewRole { get; set; } = null!;
    }
}