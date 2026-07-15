using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class AddIdImageDTO
    {
        [Required(ErrorMessage = "IdentificationImage is required.")]
        public IFormFile? IdentificationImage { get; set; }

    }
}
