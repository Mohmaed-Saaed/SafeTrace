using SafeTrace.Application.DTOs.Cases.Response;

namespace SafeTrace.Application.DTOs.UrgentCase.Response
{
    public class UrgentCaseDetailDto : CaseDetailBaseDto
    {
        public DateTime? EndDate { get; set; }
        public DateTime? LimitReachDate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
