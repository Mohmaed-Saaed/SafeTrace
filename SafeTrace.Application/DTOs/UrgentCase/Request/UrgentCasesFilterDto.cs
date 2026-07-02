using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.UrgentCase.Request
{
    public class UrgentCasesFilterDto : CasesFilterBaseDto
    {
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }
 
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }
 
        [Range(100, 500_000, ErrorMessage = "Radius must be between 100 m and 500,000 m.")]
        public double RadiusInMeters { get; set; } = 50000;
    }
}

