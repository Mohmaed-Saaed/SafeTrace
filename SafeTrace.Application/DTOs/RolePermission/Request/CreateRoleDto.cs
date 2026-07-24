using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.RolePermission.Request
{
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "اسم الدور مطلوب.")]
        [MaxLength(50, ErrorMessage = "اسم الدور لا يمكن أن يتجاوز 50حرف.")]
        public string RoleName { get; set; } = null!;
    }
}