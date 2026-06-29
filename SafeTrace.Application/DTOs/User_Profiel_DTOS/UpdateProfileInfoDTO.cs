using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class UpdateProfileInfoDTO
    {
        [Required(ErrorMessage = "First Name is required.")]
        [MaxLength(100, ErrorMessage = "First Name can't exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [MaxLength(100, ErrorMessage = "Last Name can't exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;


        public double? HomeLatitude { get; set; }
        public double? HomeLongitude { get; set; }

        [Required(ErrorMessage = "IdentificationImage is required.")]
        public IFormFile? IdentificationImage { get; set; }

        public IFormFile? ProfileImage { get; set; }


        [Required(ErrorMessage = "CurrentPassword is required.")]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string? NewPassword { get; set; }
    }
}
