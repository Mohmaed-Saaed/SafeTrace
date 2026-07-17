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
    [Authorize]
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
        // [HasPermission(Permissions.AiMatching.Search)] // يمكنك تفعيلها إذا كنت تريد تقييدها بصلاحية معينة غير مجرد تسجيل الدخول
        public async Task<IActionResult> SearchMatchingCases([FromForm] AiMatchingDto aiMatchingDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("تعذر التحقق من هوية المستخدم.");

            var response = await _aiMatchingService.GetMatchingCasesAsync(aiMatchingDto.Image, userId);
            return Ok(response);
        }
    }
}