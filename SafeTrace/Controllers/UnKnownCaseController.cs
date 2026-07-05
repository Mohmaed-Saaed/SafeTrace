using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnknownCaseController : ControllerBase
    {
        private readonly IUnknownCaseService _unKnownServiceCase;

        public UnknownCaseController(IUnknownCaseService unKnownServiceCase)
        {
            _unKnownServiceCase = unKnownServiceCase;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] UnknownCasesFilterDto filter)
        {
            return Ok(await _unKnownServiceCase.GetAllAsync(filter));
        }

        [HttpGet("admin")]
        [HasPermission(Permissions.UnknownCases.GetAll)]
        public async Task<IActionResult> AdminGetAll([FromQuery] UnknownCasesFilterDto filter)
        {
            return Ok(await _unKnownServiceCase.AdminGetAllAsync(filter));
        }

        [HttpGet("my-cases")]
        [HasPermission(Permissions.UnknownCases.GetMyCases)]
        public async Task<IActionResult> GetMyCases([FromQuery] UnknownCasesFilterDto filter)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _unKnownServiceCase.GetMyCasesAsync(CurrentUserId, filter));
        }

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _unKnownServiceCase.GetByIdAsync(id));
        }

        [HttpGet("admin/{id:long}")]
        [HasPermission(Permissions.LongTermCases.GetById)]
        public async Task<IActionResult> AdminGetById(long id)
        {
            return Ok(await _unKnownServiceCase.AdminGetByIdAsync(id));
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

            var result = await _unKnownServiceCase.UpdateUnknownCaseAsync(id, dto, userId);

            return Ok(result);
        }

        [HttpPut("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id)
        {
            await _unKnownServiceCase.ApproveAsync(id);
            return NoContent();
        }

        [HttpPut("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id)
        {
            await _unKnownServiceCase.RejectAsync(id);
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> SoftDelete(long id)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            await _unKnownServiceCase.SoftDeleteAsync(id, CurrentUserId, IsAdmin);
            return NoContent();
        }

        [HttpPut("{id:long}/mark-as-found")]
        public async Task<IActionResult> MarkAsFound(long id, FoundPersonInfoRequestDto foundPersonInfo)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            await _unKnownServiceCase.MarkAsFoundAsync(id, CurrentUserId, foundPersonInfo, isAdmin: IsAdmin);
            return NoContent();
        }

        [HttpDelete("{id:long}/permanent")]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            await _unKnownServiceCase.PermanentDeleteAsync(id);
            return NoContent();
        }
    }
}