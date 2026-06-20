using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class AssignUserPermissionsDto
    {
        [Required]
        public List<string> SelectedPermissions { get; set; } = new();
    }
}