namespace SafeTrace.Application.DTOs.Cases.Request
{
    public class FoundPersonInfoRequestDto
    {
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Government is required.")]
        [ValidEgyptianGovernorate(ErrorMessage = "Invalid Governorate.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Government must be between 2 and 100 characters.")]
        public string Government { get; set; } = null!;

        [Required(ErrorMessage = "City is required.")]
        [ValidEgyptianCity(nameof(Government), ErrorMessage = "Invalid City for the selected Governorate.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200, ErrorMessage = "Street cannot exceed 200 characters.")]
        public string Street { get; set; } = null!;

        [Required(ErrorMessage = "Founded date is required.")]
        [PastDate(ErrorMessage = "Founded date cannot be in the future.")]
        [DataType(DataType.Date)]
        public DateOnly FoundedAt { get; set; }
    }
}
