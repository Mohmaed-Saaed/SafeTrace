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
        // [HasPermission(Permissions.UrgentCases.GetAll)]
        [AllowAnonymous]

        public async Task<IActionResult> AdminGetAll([FromQuery] UrgentCaseFilterDto filter)
        {
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.AdminGetAllAsync(filter));
        }

        [HttpGet("Detail")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _urgentCaseService.GetByIdAsync(id));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        // [HasPermission(Permissions.UrgentCases.Create)]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromForm] UrgentCaseCreateDto dto)
        {
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.CreateAsync("User1", dto));
        }

        [HttpPut]
        // [HasPermission(Permissions.UrgentCases.Update)]
        [AllowAnonymous]
        public async Task<IActionResult> Update([FromForm] UrgentCaseUpdateDto dto)
        {
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.UpdateAsync("User1", dto));
        }

        [HttpDelete("{id}")]
        // [HasPermission(Permissions.UrgentCases.SoftDelete)]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(long id)
        {
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.DeleteAsync("User1", id));
        }

        [HttpDelete("permanent/{id}")]
        // [HasPermission(Permissions.UrgentCases.HardDelete)]
        [AllowAnonymous]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            return Ok(await _urgentCaseService.PermanentDeleteAsync(id));
        }

        [HttpPost("{id}/mark-founded")]
        // [HasPermission(Permissions.UrgentCases.MarkAsFounded)]
        [AllowAnonymous]
        public async Task<IActionResult> MarkAsFounded(long id)
        {
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.MarkAsFoundedAsync("User1", id)); 
        }
    }
}
