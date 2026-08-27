using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.FacebookPosts.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices.IFacebookIntegration;

namespace SafeTrace.API.Controllers.Tests
{
    [ApiController]
    [Route("api/tests/bedrock")]
    public class BedrockTestController : ControllerBase
    {
        private readonly IBedrockAiCaseAnalyzerService _bedrockAiCaseAnalyzerService;

        public BedrockTestController(IBedrockAiCaseAnalyzerService bedrockAiCaseAnalyzerService)
        {
            _bedrockAiCaseAnalyzerService = bedrockAiCaseAnalyzerService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(
            [FromBody] AnalyzePostTextTestRequest request)
        {
            var result = await _bedrockAiCaseAnalyzerService.AnalyzeAsync(request.Text);

            return Ok(ApiResponse<SocialPostAiResultDto>.Ok(
                result,
                "Bedrock AI case analysis completed successfully."));
        }
    }
}
