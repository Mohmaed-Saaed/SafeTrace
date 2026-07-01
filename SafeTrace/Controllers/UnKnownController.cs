using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnKnownController : ControllerBase
    {
        private readonly IUnknownCaseService _unKnownServiceCase;

        public UnKnownController(IUnknownCaseService unKnownServiceCase, ICasesService casesService)
        {
            _unKnownServiceCase = unKnownServiceCase;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.UnknownCases.Create)]
        public async Task<IActionResult> CreateUnknown([FromForm] CreateUnknownDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedException("User is not authenticated.");

            var result = await _unKnownServiceCase.CreateUnknownCaseAsync(dto, userId);

            return Ok(result);
        }

        [HttpPut("{id}/UpdateUnKnownCase")]
        [HasPermission(Permissions.UnknownCases.Update)]
        public async Task<IActionResult> UpdateUnknownCase(long id, [FromForm] UpdateUnknownCaseDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _unKnownServiceCase
                .UpdateUnknownCaseAsync(id, dto, userId);

            return Ok(result);
        }
    }
}