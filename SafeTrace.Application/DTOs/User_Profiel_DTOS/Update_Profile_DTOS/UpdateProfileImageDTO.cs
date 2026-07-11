using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateProfileImageDTO
    {
        [Required(ErrorMessage = "ProfileImage  is required.")]
        public IFormFile? ProfileImage { get; set; }

    }
}
