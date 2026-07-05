using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.UrgentCase.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;
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

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] UrgentCasesFilterDto filter)
        {
            return Ok(await _urgentCaseService.GetAllAsync(filter));
        }

        [HttpGet("admin")]
        [HasPermission(Permissions.UrgentCases.GetAll)]
        public async Task<IActionResult> AdminGetAll([FromQuery] UrgentCasesFilterDto filter)
        {
            return Ok(await _urgentCaseService.AdminGetAllAsync(filter));
        }

        [HttpGet("my-cases")]
        [HasPermission(Permissions.UrgentCases.GetMyCases)]
        public async Task<IActionResult> GetMyCases([FromQuery] UrgentCasesFilterDto filter)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.GetMyCasesAsync(CurrentUserId, filter));
        }

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _urgentCaseService.GetByIdAsync(id));
        }

        [HttpGet("admin/{id:long}")]
        [HasPermission(Permissions.LongTermCases.GetById)]
        public async Task<IActionResult> AdminGetById(long id)
        {
            return Ok(await _urgentCaseService.AdminGetByIdAsync(id));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.UrgentCases.Create)]
        public async Task<IActionResult> Create([FromForm] UrgentCaseCreateDto dto)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.CreateAsync(CurrentUserId, dto));
        }

        [HttpPut]
        [HasPermission(Permissions.UrgentCases.Update)]
        public async Task<IActionResult> Update([FromForm] UrgentCaseUpdateDto dto)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.UpdateAsync(CurrentUserId, dto));
        }

        [HttpDelete("{id:long}")]
        [HasPermission(Permissions.UrgentCases.SoftDelete)]
        public async Task<IActionResult> SoftDelete(long id)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            await _urgentCaseService.SoftDeleteAsync(id, CurrentUserId, IsAdmin);
            return NoContent();
        }

        [HttpPut("{id:long}/mark-as-found")]
        [HasPermission(Permissions.UrgentCases.MarkAsFounded)]
        public async Task<IActionResult> MarkAsFound(long id, [FromBody] FoundPersonInfoRequestDto foundPersonInfo)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            await _urgentCaseService.MarkAsFoundAsync(id, CurrentUserId, foundPersonInfo, isAdmin: IsAdmin);

            return NoContent();
        }

        [HttpDelete("{id:long}/permanent")]
        [HasPermission(Permissions.UrgentCases.HardDelete)]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            await _urgentCaseService.PermanentDeleteAsync(id);
            return NoContent();
        }
    }
}