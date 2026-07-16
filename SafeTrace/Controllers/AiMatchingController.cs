using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.AiMatching.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.RateLimiting.DisableRateLimiting]
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
        [HasPermission(Permissions.AiMatching.Search)]
        public async Task<IActionResult> SearchMatchingCases([FromForm] AiMatchingDto aiMatchingDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("تعذر التحقق من هوية المستخدم.");

            var response = await _aiMatchingService.GetMatchingCasesAsync(aiMatchingDto.Image, userId);
            return Ok(response);
        }

        //test
        [HttpPost("searchtest")]
        public async Task<IActionResult> Get([FromForm] AiMatchingDto t)
        {
            var response = await _faceRecognitionService.SearchByImageAsync(t.Image);
            return Ok(response);
        }

        [HttpPost("save")]
        public async Task<IActionResult> save([FromForm] AiMatchingDto t)
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