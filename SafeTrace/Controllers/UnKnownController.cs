using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Services;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnKnownController : ControllerBase
    {
        private readonly IUnknownCaseService _unKnownServiceCase;

        public UnKnownController(IUnknownCaseService unKnownServiceCase)
        {
            _unKnownServiceCase = unKnownServiceCase;
        }
        [HttpPost]
        public async Task<IActionResult> CreateUnknown([FromForm] CreateUnknownDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _unKnownServiceCase.CreateUnknownCaseAsync(dto, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }


    }
}
