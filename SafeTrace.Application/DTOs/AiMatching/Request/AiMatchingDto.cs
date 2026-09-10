using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using SafeTrace.Application.Common.Validators.Attributes;

namespace SafeTrace.Application.DTOs.AiMatching.Request
{
    public class AiMatchingDto
    {
        [Required(ErrorMessage = "Image is required.")]
        [AllowedPhotoTypes(ErrorMessage = "Only JPG, JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        public IFormFile Image { get; set; } = null!;
    }
}