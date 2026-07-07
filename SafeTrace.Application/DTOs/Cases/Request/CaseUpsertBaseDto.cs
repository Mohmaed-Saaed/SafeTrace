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
        [Range(0, 150, ErrorMessage = "Age must be between 0 and 150.")]
        public int Age { get; set; }
                

        [Required(ErrorMessage = "Government is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Government must be between 2 and 200 characters.")]
        public string Government { get; set; } = null!;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "City must be between 2 and 200 characters.")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(500, ErrorMessage = "Street cannot exceed 500 characters.")]
        public string? Street { get; set; }

        [Required(ErrorMessage = "Primary image is required.")]
        public IFormFile PrimaryImage { get; set; } = null!;

        // OPTIONAL FIELDS
        
        [StringLength(100, ErrorMessage = "Second name cannot exceed 100 characters.")]
        public string? SName { get; set; }

        [StringLength(100, ErrorMessage = "Third name cannot exceed 100 characters.")]
        public string? TName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
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