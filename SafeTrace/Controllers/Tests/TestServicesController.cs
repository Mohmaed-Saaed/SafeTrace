using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.FacebookPosts.Response;
using SafeTrace.Application.DTOs.Geocoding.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.IFacebookIntegration;

namespace SafeTrace.API.Controllers.Tests
{
    [ApiController]
    [Route("api/tests")]
    public class TestServicesController : ControllerBase
    {
        private readonly IAiCaseAnalyzerService _aiCaseAnalyzerService;
        private readonly IFacebookGraphService _facebookGraphService;
        private readonly IGeocodingService _geocodingService;

        public TestServicesController(
            IAiCaseAnalyzerService aiCaseAnalyzerService,
            IFacebookGraphService facebookGraphService,
            IGeocodingService geocodingService)
        {
            _aiCaseAnalyzerService = aiCaseAnalyzerService;
            _facebookGraphService = facebookGraphService;
            _geocodingService = geocodingService;
        }

        [HttpPost("ai-case-analyzer")]
        public async Task<IActionResult> TestAiCaseAnalyzer(
            [FromBody] AnalyzePostTextTestRequest request)
        {
            var result = await _aiCaseAnalyzerService.AnalyzeAsync(request.Text);
            return Ok(ApiResponse<SocialPostAiResultDto>.Ok(
                result,
                "AI case analysis completed successfully."));
        }

        [HttpPost("facebook-graph/connect")]
        public async Task<IActionResult> TestFacebookGraphConnect(
            [FromBody] FacebookGraphConnectTestRequest request)
        {
            var result = await _facebookGraphService.ConnectPageAsync(
                request.FacebookPageId,
                request.PageAccessToken);

            return Ok(ApiResponse<FacebookPageConnectionResultDto>.Ok(
                result,
                "Facebook page connection completed successfully."));
        }

        [HttpPost("facebook-graph/posts")]
        public async Task<IActionResult> TestFacebookGraphGetPosts(
            [FromBody] FacebookGraphGetPostsTestRequest request)
        {
            var result = await _facebookGraphService.GetNewPostsAsync(
                request.FacebookPageId,
                request.PageAccessToken,
                request.Since);

            return Ok(ApiResponse<IReadOnlyList<FacebookPostDto>>.Ok(
                result,
                "Facebook posts fetched successfully."));
        }

        [HttpGet("geocoding")]
        public async Task<IActionResult> TestGeocoding(
            [FromQuery] string? government,
            [FromQuery] string? city,
            [FromQuery] string? street)
        {
            var result = await _geocodingService.GeocodeAsync(
                government,
                city,
                street);

            return Ok(ApiResponse<GeocodingResultDto?>.Ok(
                result,
                "Geocoding completed successfully."));
        }
    }

    public class AnalyzePostTextTestRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class FacebookGraphConnectTestRequest
    {
        public string FacebookPageId { get; set; } = string.Empty;
        public string PageAccessToken { get; set; } = string.Empty;
    }

    public class FacebookGraphGetPostsTestRequest
    {
        public string FacebookPageId { get; set; } = string.Empty;
        public string PageAccessToken { get; set; } = string.Empty;
        public DateTimeOffset? Since { get; set; }
    }
}
