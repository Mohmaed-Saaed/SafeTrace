using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.UrgentCase.Request
{
    public class UrgentCaseCreateDto : CaseCreateBaseDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string LName { get; set; } = null!;
        
        [Required(ErrorMessage = "Relation is required.")]
        [EnumDataType(typeof(RelationType), ErrorMessage = "Invalid relation type.")]
        public RelationType Relation { get; set; }

        [Required(ErrorMessage = "Event date is required.")]
        [DataType(DataType.DateTime)]
        [PastDate(ErrorMessage = "Event date cannot be in the future.")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }
    }
}