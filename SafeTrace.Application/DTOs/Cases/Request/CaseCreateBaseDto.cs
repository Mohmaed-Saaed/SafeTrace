using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.Cases.Request
{
    public abstract class CaseCreateBaseDto : CaseUpsertBaseDto
    {
        [Required(ErrorMessage = "Primary image is required.")]
        [AllowedPhotoTypes(ErrorMessage = "Only JPG, JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        public IFormFile PrimaryImage { get; set; } = null!;
    }
}
