using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "First Name is required.")]
        [MaxLength(100, ErrorMessage = "First Name can't exceed 100 characters")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required.")]
        [MaxLength(100, ErrorMessage = "Last Name can't exceed 100 characters")]
        public string LName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "Invalid Egyptian phone number.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; } = null!;
    }
}