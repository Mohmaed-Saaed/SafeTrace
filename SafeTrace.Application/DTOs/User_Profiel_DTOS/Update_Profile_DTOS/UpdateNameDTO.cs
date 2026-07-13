

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateNameDTO
    {
        [Required(ErrorMessage = "First Name is required.")]
        [MaxLength(100, ErrorMessage = "First Name can't exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [MaxLength(100, ErrorMessage = "Last Name can't exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;
    }
}
