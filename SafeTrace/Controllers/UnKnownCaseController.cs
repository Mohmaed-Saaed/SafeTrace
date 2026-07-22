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

        [HttpGet("GetCases")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] UnknownCasesFilterDto filter)
        {
            return Ok(await _unKnownServiceCase.GetAllAsync(filter));
        }

        [HttpGet("Admin/GetCases")]
        [HasPermission(Permissions.Cases.GetAll)]
        public async Task<IActionResult> AdminGetAll([FromQuery] UnknownCasesFilterDto filter)
        {
            return Ok(await _unKnownServiceCase.AdminGetAllAsync(filter));
        }

        [HttpGet("GetCaseDetails/{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _unKnownServiceCase.GetByIdAsync(id));
        }

        [HttpGet("Admin/GetCaseDetails/{id:long}")]
        [HasPermission(Permissions.UnknownCases.GetById)]
        public async Task<IActionResult> AdminGetById(long id)
        {
            return Ok(await _unKnownServiceCase.AdminGetByIdAsync(id));
        }
        [HttpPost("CreateCase")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.UnknownCases.Create)]
        public async Task<IActionResult> CreateUnknown(
            [FromForm] CreateUnknownDto dto,
            [FromQuery] bool forceCreate = false)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _unKnownServiceCase.CreateUnknownCaseAsync(CurrentUserId, dto, forceCreate));
        }

        [HttpPut("UpdateCase/{id:long}")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.UnknownCases.Update)]
        public async Task<IActionResult> UpdateUnknownCase(long id, [FromForm] UpdateUnknownCaseDto dto)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _unKnownServiceCase.UpdateUnknownCaseAsync(id, CurrentUserId, dto));
        }

        [HttpPut("Approve/{id:long}")]
        [HasPermission(Permissions.UnknownCases.Approve)]
        public async Task<IActionResult> Approve(long id)
        {
            return Ok(await _unKnownServiceCase.ApproveAsync(id));
        }

        [HttpPut("Reject/{id:long}")]
        [HasPermission(Permissions.UnknownCases.Reject)]
        public async Task<IActionResult> Reject(long id)
        {
            return Ok(await _unKnownServiceCase.RejectAsync(id));
        }

        [HttpDelete("Delete/{id:long}")]
        [HasPermission(Permissions.UnknownCases.SoftDelete)]
        public async Task<IActionResult> SoftDelete(long id)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _unKnownServiceCase.SoftDeleteAsync(id, CurrentUserId));
        }

        [HttpPut("MarkAsFound/{id:long}")]
        [HasPermission(Permissions.UnknownCases.MarkAsFounded)]
        public async Task<IActionResult> MarkAsFound(long id, FoundPersonInfoRequestDto foundPersonInfo)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _unKnownServiceCase.MarkAsFoundAsync(id, CurrentUserId, foundPersonInfo));
        }

        [HttpDelete("PermanentDeletion/{id:long}")]
        [HasPermission(Permissions.UnknownCases.HardDelete)]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            return Ok(await _unKnownServiceCase.PermanentDeleteAsync(id));
        }
    }
}