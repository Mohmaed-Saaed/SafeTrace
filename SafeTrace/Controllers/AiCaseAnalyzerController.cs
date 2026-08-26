using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.AiCaseAnalyzer.Request;
using SafeTrace.Application.DTOs.AiCaseAnalyzer.Response;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.API.Controllers
{
    public sealed class AiCaseAnalyzerController : BaseApiController
    {
        private readonly IAiCaseAnalyzerService _aiCaseAnalyzerService;

        public AiCaseAnalyzerController(
            IAiCaseAnalyzerService aiCaseAnalyzerService)
        {
            _aiCaseAnalyzerService = aiCaseAnalyzerService;
        }

        [HttpPost("analyze")]
        [ProducesResponseType<SocialPostAiResultDto>(StatusCodes.Status200OK)]
        public async Task<ActionResult<SocialPostAiResultDto>> Analyze(
            [FromBody] SocialPostAiInputDto input)
        {
            var result = await _aiCaseAnalyzerService.AnalyzeAsync(input);

            return Ok(result);
        }
    }
}