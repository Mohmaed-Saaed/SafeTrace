using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.AiMatching.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;

namespace SafeTrace.API.Controllers
{
    [EnableRateLimiting(RateLimitPolicies.AiLimit)]
    public class AiMatchingController : BaseApiController
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
            var response = await _aiMatchingService.GetMatchingCasesAsync(aiMatchingDto.Image, CurrentUserId);
            return Ok(response);
        }
    }
}