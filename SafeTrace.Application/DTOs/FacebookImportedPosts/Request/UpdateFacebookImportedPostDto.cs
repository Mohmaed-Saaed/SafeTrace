namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Request
{
    public sealed class UpdateFacebookImportedPostDto
    {
        [EnumDataType(typeof(SocialPostClassification), ErrorMessage = "Invalid classification.")]
        public SocialPostClassification? Classification { get; set; }

        [StringLength(60)]
        [ArabicText(ErrorMessage = "First name must contain Arabic letters and spaces only.")]
        public string? FName { get; set; }

        [StringLength(60)]
        [ArabicText(ErrorMessage = "Second name must contain Arabic letters and spaces only.")]
        public string? SName { get; set; }

        [StringLength(60)]
        [ArabicText(ErrorMessage = "Third name must contain Arabic letters and spaces only.")]
        public string? TName { get; set; }

        [StringLength(60)]
        [ArabicText(ErrorMessage = "Last name must contain Arabic letters and spaces only.")]
        public string? LName { get; set; }

        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender.")]
        public Gender? Gender { get; set; }

        [Range(1, 120)]
        public int? Age { get; set; }

        [StringLength(100)]
        [ValidEgyptianGovernorate(ErrorMessage = "Invalid Governorate.")]
        public string? Government { get; set; }

        [StringLength(100)]
        [ValidEgyptianCity(nameof(Government), ErrorMessage = "Invalid City for the selected Governorate.")]
        public string? City { get; set; }

        [StringLength(200)]
        public string? Street { get; set; }

        public DateOnly? EventDate { get; set; }

        [StringLength(15)]
        [EgyptianPhone(ErrorMessage = "Please enter a valid Egyptian mobile number.")]
        public string? CommunicationPhone { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [EnumDataType(typeof(RelationType), ErrorMessage = "Invalid relation type.")]
        public RelationType? Relation { get; set; }

        [Range(-90d, 90d)]
        public double? Latitude { get; set; }

        [Range(-180d, 180d)]
        public double? Longitude { get; set; }
    }
}
