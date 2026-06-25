using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.AiMatching.Request;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiMatchingController : ControllerBase
    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IAIMatchingService _aiMatchingService;
        public AiMatchingController(IFaceRecognitionService faceRecognitionService, IAIMatchingService aiMatchingService)
        {
            _faceRecognitionService = faceRecognitionService;
            _aiMatchingService = aiMatchingService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchMatchingCases([FromForm] AiMatchingDto aiMatchingDto)
        {
            var response = await _aiMatchingService.GetMatchingCasesAsync(aiMatchingDto.Image);
            return Ok(response);
        }

        //test
        [HttpPost("searchtest")]
        public async Task<IActionResult> Get([FromForm] test t)
        {
            var response = await _faceRecognitionService.SearchByImageAsync(t.Image);
            return Ok(response);
        }

        [HttpPost("save")]
        public async Task<IActionResult> save([FromForm] test t)
        {
            var response = await _faceRecognitionService.IndexFaceAsync(t.Image);
            return Ok(response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> delete(string faceId)
        {
            var response = await _faceRecognitionService.DeleteFaceAsync(faceId);
            return Ok(response);
        }
    }
}