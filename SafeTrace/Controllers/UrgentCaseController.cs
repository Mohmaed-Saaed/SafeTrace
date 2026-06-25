using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrgentCaseController : ControllerBase
    {
        private readonly IUrgentCaseService _urgentCaseService;

        public UrgentCaseController(IUrgentCaseService urgentCaseService)
        {
            _urgentCaseService = urgentCaseService;
        }

        [HttpGet("Cases")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] UrgentCaseFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");
            return Ok(await _urgentCaseService.GetAllAsync(userId, filter));
        }

        [HttpGet("admin/Cases")]
        [HasPermission(Permissions.UrgentCases.GetAll)]

        public async Task<IActionResult> AdminGetAll([FromQuery] UrgentCaseFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.AdminGetAllAsync(userId, filter));
        }

        [HttpGet("Detail")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _urgentCaseService.GetByIdAsync(id));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.UrgentCases.Create)]
        public async Task<IActionResult> Create([FromForm] UrgentCaseCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.CreateAsync(userId, dto));
        }

        [HttpPut]
        [HasPermission(Permissions.UrgentCases.Update)]
        public async Task<IActionResult> Update([FromBody] UrgentCaseUpdateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.UpdateAsync(userId, dto));
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.UrgentCases.SoftDelete)]
        public async Task<IActionResult> Delete(long id)
        {
            return Ok(await _urgentCaseService.DeleteAsync(id));
        }

        [HttpDelete("permanent/{id}")]
        [HasPermission(Permissions.UrgentCases.HardDelete)]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            return Ok(await _urgentCaseService.PermanentDeleteAsync(id));
        }

        [HttpPost("{id}/mark-founded")]
        [HasPermission(Permissions.UrgentCases.MarkAsFounded)]
        public async Task<IActionResult> MarkAsFounded(long id)
        {
            return Ok(await _urgentCaseService.MarkAsFoundedAsync(id)); 
        }
    }
}
