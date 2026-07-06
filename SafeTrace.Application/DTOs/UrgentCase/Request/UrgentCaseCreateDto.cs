using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.UrgentCase.Request
{
    public class UrgentCaseCreateDto
    {
        [Required(ErrorMessage = "Gender is required.")]
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Government is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Government must be between 2 and 100 characters.")]
        public string Government { get; set; } = null!;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Street must be between 2 and 200 characters.")]
        public string Street { get; set; } = null!;

        [StringLength(60, ErrorMessage = "First name cannot exceed 60 characters.")]
        public string? FName { get; set; }

        [StringLength(60, ErrorMessage = "Second name cannot exceed 60 characters.")]
        public string? SName { get; set; }

        [StringLength(60, ErrorMessage = "Third name cannot exceed 60 characters.")]
        public string? TName { get; set; }

        [StringLength(60, ErrorMessage = "Last name cannot exceed 60 characters.")]
        public string? LName { get; set; }

        [Required(ErrorMessage = "Age is required.")]
        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
        public int Age { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? CommunicationPhone { get; set; }

        [Required(ErrorMessage = "Relation type is required.")]
        [EnumDataType(typeof(RelationType), ErrorMessage = "Invalid relation type.")]
        public RelationType Relation { get; set; }

        [Required(ErrorMessage = "Event date is required.")]
        [DataType(DataType.DateTime)]
        [PastDate(ErrorMessage = "Event date cannot be in the future.")]
        public DateTime EventDate { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        [MaxPhotoCount(5, ErrorMessage = "You can upload a maximum of 5 photos.")]
        [AllowedPhotoTypes(ErrorMessage = "Only JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        public List<IFormFile>? Photos { get; set; }
    }
}


