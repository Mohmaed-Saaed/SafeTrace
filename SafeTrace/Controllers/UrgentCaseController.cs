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
    public class UrgentCaseController : BaseApiController
    {
        private readonly IUrgentCaseService _urgentCaseService;

        public UrgentCaseController(IUrgentCaseService urgentCaseService)
        {
            _urgentCaseService = urgentCaseService;
        }

        [HttpGet("GetCases")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] UrgentCasesFilterDto filter)
        {
            return Ok(await _urgentCaseService.GetAllAsync(filter));
        }

        [HttpGet("Admin/GetCases")]
        [HasPermission(Permissions.Cases.GetAll)]
        public async Task<IActionResult> AdminGetAll([FromQuery] UrgentCasesFilterDto filter)
        {
            return Ok(await _urgentCaseService.AdminGetAllAsync(filter));
        }

        [HttpGet("GetCaseDetails/{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _urgentCaseService.GetByIdAsync(id));
        }

        [HttpGet("Admin/GetCaseDetails/{id:long}")]
        [HasPermission(Permissions.UrgentCases.GetById)]
        public async Task<IActionResult> AdminGetById(long id)
        {
            return Ok(await _urgentCaseService.AdminGetByIdAsync(id));
        }
        [HttpPost("CreateCase")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> Create([FromForm] UrgentCaseCreateDto dto, [FromQuery] bool forceCreate = false)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.CreateAsync(CurrentUserId, dto, forceCreate));
        }
        [HttpPut("UpdateCase/{id:long}")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> Update(long id, [FromForm] UrgentCaseUpdateDto dto)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.UpdateAsync(id, CurrentUserId, dto));
        }

        [HttpDelete("Delete/{id:long}")]
        [Authorize]
        public async Task<IActionResult> SoftDelete(long id)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.SoftDeleteAsync(id, CurrentUserId));
        }

        [HttpPut("{id:long}/mark-as-found")]
        [HasPermission(Permissions.UrgentCases.MarkAsFounded)]
        public async Task<IActionResult> MarkAsFound(long id, [FromBody] FoundPersonInfoRequestDto foundPersonInfo)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.MarkAsFoundAsync(id, CurrentUserId, foundPersonInfo));
        }

        [HttpDelete("{id:long}/permanent")]
        [HasPermission(Permissions.UrgentCases.HardDelete)]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            return Ok(await _urgentCaseService.PermanentDeleteAsync(id));
        }

        [HttpGet("CreationStatus")]
        [Authorize]
        public async Task<IActionResult> GetCreationStatus()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.GetUrgentCreationStatusAsync(CurrentUserId));
        }

        [HttpGet("MyCaseDetails/{id:long}")]
        [Authorize]
        public async Task<IActionResult> GetMyCaseById(long id)
        {
            if (CurrentUserId == null)
                throw new UnauthorizedException("?? ??? ?????? ??? ???? ????????.");

            return Ok(await _urgentCaseService.GetMyCaseByIdAsync(id, CurrentUserId));
        }

    }
}