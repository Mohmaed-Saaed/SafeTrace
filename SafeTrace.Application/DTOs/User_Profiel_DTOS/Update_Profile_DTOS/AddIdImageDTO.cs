using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Validators.Attributes;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class AddIdImageDTO
    {
        [Required(ErrorMessage = "IdentificationImage is required.")]
        [AllowedPhotoTypes(ErrorMessage = "Only JPG, JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        public IFormFile? IdentificationImage { get; set; }
    }
}
