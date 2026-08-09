using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.UrgentCase.Request
{
    public class UrgentCaseCreateDto : CaseCreateBaseDto
    {
        [Required(ErrorMessage = "Event date is required.")]
        [UrgentEventDate(24)]
        [DataType(DataType.Date)]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [ArabicText(ErrorMessage = "First name must contain Arabic letters and spaces only.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 60 characters.")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required.")]
        [ArabicText(ErrorMessage = "Last name must contain Arabic letters and spaces only.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 60 characters.")]
        public string LName { get; set; } = null!;
        
        [Required(ErrorMessage = "Relation is required.")]
        [EnumDataType(typeof(RelationType), ErrorMessage = "Invalid relation type.")]
        public RelationType Relation { get; set; }

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }
    }
}
