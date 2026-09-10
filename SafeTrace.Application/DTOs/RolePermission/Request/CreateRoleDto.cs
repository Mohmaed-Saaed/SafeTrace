using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.RolePermission.Request
{
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "اسم الدور مطلوب.")]
        [MinLength(2, ErrorMessage = "اسم الدور لا يمكن أن يقل عن حرفين.")]
        [MaxLength(50, ErrorMessage = "اسم الدور لا يمكن أن يتجاوز 50حرف.")]
        [RegularExpression(@"^[\u0600-\u06FF]+$", ErrorMessage = "يجب كتابة اسم الدور باللغة العربية فقط (بدون مسافات، أرقام، أو رموز خاصة).")]
        public string RoleName { get; set; } = null!;
    }
}