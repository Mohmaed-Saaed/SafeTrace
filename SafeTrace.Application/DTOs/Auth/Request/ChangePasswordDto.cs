using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth.Request
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current Password is required.")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "New Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "New Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$", ErrorMessage = "New Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string NewPassword { get; set; } = null!;
    }
}