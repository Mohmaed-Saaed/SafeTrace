using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.Exceptions;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.DTOs.LongTermCase.Request;
using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LongTermCaseController : ControllerBase
    {
        private readonly ILongTermCaseService _service;

        public LongTermCaseController(ILongTermCaseService service)
        {
            _service = service;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet("GetCases")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] LongTermCaseFilterDto filter)
        {
            return Ok(await _service.GetAllAsync(filter));
        }
        
        [HttpGet("Admin/GetCases")]
        [HasPermission(Permissions.LongTermCases.GetAll)]
        public async Task<IActionResult> AdminGetAll([FromQuery] LongTermCaseFilterDto filter)
        {
            return Ok(await _service.AdminGetAllAsync(filter));
        }

        [HttpGet("GetMyCases")]
        [HasPermission(Permissions.LongTermCases.GetMyCases)]
        public async Task<IActionResult> GetMyCases([FromQuery] LongTermCaseFilterDto filter)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");
            return Ok(await _service.GetMyCasesAsync(CurrentUserId, filter));
        }

        [HttpGet("GetCaseDetails/{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _service.GetByIdAsync(id));
        }

        [HttpGet("Admin/GetCaseDetails/{id:long}")]
        [HasPermission(Permissions.LongTermCases.GetById)]
        public async Task<IActionResult> AdminGetById(long id)
        {
            return Ok(await _service.AdminGetByIdAsync(id));
        }

        /// <summary>
        /// إنشاء حالة جديدة — يتطلب صلاحية Create
        /// </summary>
        [HttpPost("CreateCase")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.LongTermCases.Create)]
        public async Task<IActionResult> Create([FromForm] CreateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            var id = await _service.CreateAsync(dto, CurrentUserId);
            // return CreatedAtAction(nameof(GetById), new { id }, new { id });
            return Ok();
        }

        /// <summary>
        /// تعديل حالة — المستخدم يعدل حالته، الأدمن يعدل أي حالة
        /// </summary>
        [HttpPut("UpdateCase/{id:long}")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.LongTermCases.Update)]
        public async Task<IActionResult> Update(long id, [FromForm] UpdateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            await _service.UpdateAsync(id, dto, CurrentUserId);
            return NoContent();
        }

        [HttpPut("Approve/{id:long}")]
        [HasPermission(Permissions.LongTermCases.Approve)]
        public async Task<IActionResult> Approve(long id)
        {
            await _service.ApproveAsync(id);
            return NoContent();
        }

        [HttpPut("Reject/{id:long}")]
        [HasPermission(Permissions.LongTermCases.Reject)]
        public async Task<IActionResult> Reject(long id)
        {
            await _service.RejectAsync(id);
            return NoContent();
        }

        [HttpDelete("Delete/{id:long}")]
        [HasPermission(Permissions.LongTermCases.SoftDelete)]
        public async Task<IActionResult> SoftDelete(long id)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");
            await _service.SoftDeleteAsync(id, CurrentUserId);
            return NoContent();
        }

        [HttpPut("MarkAsFound/{id:long}")]
        [HasPermission(Permissions.LongTermCases.MarkAsFounded)]
        public async Task<IActionResult> MarkAsFound(long id, FoundPersonInfoRequestDto foundPersonInfo)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");
            await _service.MarkAsFoundAsync(id, CurrentUserId, foundPersonInfo);
            return NoContent();
        }

        [HttpDelete("PermanentDeletion/{id:long}")]
        [HasPermission(Permissions.LongTermCases.HardDelete)]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            await _service.PermanentDeleteAsync(id);
            return NoContent();
        }
    }
}