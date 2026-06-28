using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.LongTermCases;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LongTermMissingCasesController : ControllerBase
    {
        private readonly ILongTermCaseService _service;

        public LongTermMissingCasesController(ILongTermCaseService service)
        {
            _service = service;
        }

        //private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        //private bool IsAdmin => User.IsInRole("Admin");
        private string CurrentUserId => "1dcc168b-80da-4909-8438-4e177016be76";
        private bool IsAdmin => true;


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] LongTermCaseFilterDto filter)
        {
            var result = await _service.GetAllAsync(filter);
            return Ok(result);
        }


        [HttpGet("founded")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFounded()
        {
            var result = await _service.GetFoundedCasesAsync();
            return Ok(result);
        }


        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id, includeDeleted: IsAdmin);
            return Ok(result);
        }


        //[Authorize]
        [HttpGet("my-cases")]
        public async Task<IActionResult> GetMyCases()
        {
            if (CurrentUserId == null) throw new UnauthorizedException("User identity not found.");

            var result = await _service.GetMyCasesAsync(CurrentUserId);
            return Ok(result);
        }


        /// <summary>
        /// Admin-only: filter by status=Pending (review queue) or status=Deleted (trash).
        /// GET /api/LongTermMissingCases/admin-cases?status=Pending
        /// GET /api/LongTermMissingCases/admin-cases?status=Deleted
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpGet("admin-cases")]
        public async Task<IActionResult> GetAdminCases([FromQuery] CaseStatus status)
        {
            var result = await _service.GetAdminCasesAsync(status);
            return Ok(result);
        }


        //[Authorize(Roles = "Verified,Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("User identity not found.");

            var id = await _service.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }


        //[Authorize]
        [HttpPut("{id:long}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(long id, [FromForm] UpdateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("User identity not found.");

            await _service.UpdateAsync(id, dto, CurrentUserId, IsAdmin);
            return NoContent();
        }


        //[Authorize]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("User identity not found.");

            await _service.DeleteAsync(id, CurrentUserId, IsAdmin);
            return NoContent();
        }


        //[Authorize(Roles = "Admin")]
        [HttpDelete("{id:long}/permanent")]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            await _service.PermanentDeleteAsync(id);
            return NoContent();
        }


        //[Authorize(Roles = "Admin")]
        [HttpPut("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id)
        {
            await _service.ApproveAsync(id);
            return NoContent();
        }


        //[Authorize(Roles = "Admin")]
        [HttpPut("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id)
        {
            await _service.RejectAsync(id);
            return NoContent();
        }


        //[Authorize]
        [HttpPut("{id:long}/mark-as-founded")]
        public async Task<IActionResult> MarkAsFounded(long id, [FromBody] MarkAsFoundedDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("User identity not found.");

            await _service.MarkAsFoundedAsync(id, dto, CurrentUserId, IsAdmin);
            return NoContent();
        }
    }
}