using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.Cases.Request
{
    public abstract class CaseUpsertBaseDto
    {
        // REQUIRED FIELDS (Both Create & Update)

        [Required(ErrorMessage = "Gender is required.")]
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Age is required.")]
        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Government is required.")]
        [ArabicText(ErrorMessage = "Government must contain Arabic letters and spaces only.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Government must be between 2 and 100 characters.")]
        public string Government { get; set; } = null!;

        [Required(ErrorMessage = "City is required.")]
        [ArabicText(ErrorMessage = "City must contain Arabic letters and spaces only.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200, ErrorMessage = "Street cannot exceed 200 characters.")]
        public string Street { get; set; } = null!;

        // OPTIONAL FIELDS

        [StringLength(60, ErrorMessage = "Second name cannot exceed 60 characters.")]
        [ArabicText(ErrorMessage = "Second name must contain Arabic letters and spaces only.")]
        public string? SName { get; set; }

        [StringLength(60, ErrorMessage = "Third name cannot exceed 60 characters.")]
        [ArabicText(ErrorMessage = "Third name must contain Arabic letters and spaces only.")]
        public string? TName { get; set; }

        [EgyptianPhone(ErrorMessage = "Please enter a valid Egyptian mobile number.")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
        public string? CommunicationPhone { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        // PHOTO MANAGEMENT

        [MaxPhotoCount(4, ErrorMessage = "You can upload a maximum of 4 additional photos.")]
        [AllowedPhotoTypes(ErrorMessage = "Only JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        public List<IFormFile>? AdditionalImages { get; set; }

        public IFormFile? Video { get; set; }
    }
}
