using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.FacebookPages.Request
{
    public class FacebookPageFilterDto
    {
        public string? Search { get; set; }
        public FacebookIntegrationStatus? IntegrationStatus { get; set; }
    }
}
