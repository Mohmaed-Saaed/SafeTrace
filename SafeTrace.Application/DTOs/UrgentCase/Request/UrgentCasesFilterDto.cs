using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.UrgentCase.Request
{
    public class UrgentCasesFilterDto : CasesFilterBaseDto
    {
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }
 
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }
 
        [Range(1, 1000, ErrorMessage = "Radius must be between 1 km and 1000 km.")]
        public double? RadiusInKm { get; set; }
    }
}

