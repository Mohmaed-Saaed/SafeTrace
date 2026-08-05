using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.Cases.Request
{
    public class CaseUpdateBaseDto : CaseUpsertBaseDto
    {
        [AllowedPhotoTypes(ErrorMessage = "Only JPG, JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        public IFormFile? PrimaryImage { get; set; }

        [MaxPhotoCount(5, ErrorMessage = "You can upload a maximum of 5 new photos.")]
        [AllowedPhotoTypes(ErrorMessage = "Only JPG, JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        public List<IFormFile>? NewPhotos { get; set; }

        [MinListLength(1, ErrorMessage = "Deleted photo IDs cannot be empty.")]
        [PositiveIds(ErrorMessage = "All photo IDs must be positive numbers.")]
        public List<long>? DeletedPhotoIds { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Primary photo ID must be a positive number.")]
        public long? PrimaryPhotoId { get; set; }
    }
}
