using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class ChangeUserRoleDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required(ErrorMessage = "اسم الدور الجديد مطلوب.")]
        [MaxLength(100, ErrorMessage = "اسم الدور لا يمكن أن يتجاوز 100 حرف.")]
        public string NewRole { get; set; } = null!;
    }
}