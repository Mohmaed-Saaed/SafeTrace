using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class ChangeUserRoleDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required(ErrorMessage = "اسم الدور الجديد مطلوب.")]
        [MaxLength(50, ErrorMessage = "اسم الدور لا يمكن أن يتجاوز 50 حرف.")]
        public string NewRole { get; set; } = null!;
    }
}