using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.RolePermission.Request
{
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "اسم الدور مطلوب.")]
        [MaxLength(100, ErrorMessage = "اسم الدور لا يمكن أن يتجاوز 100 حرف.")]
        public string RoleName { get; set; } = null!;
    }
}