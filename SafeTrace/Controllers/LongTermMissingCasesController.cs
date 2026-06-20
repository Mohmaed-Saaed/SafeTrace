using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.LongTermCases;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
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
        private string CurrentUserId => "test-user-id";
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


        // Admin only: cases users have soft-deleted, kept around for review (restore or permanent delete)
        //[Authorize(Roles = "Admin")]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var result = await _service.GetDeletedCasesAsync();
            return Ok(result);
        }


        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            // Admins can also open soft-deleted cases, everyone else only sees non-deleted ones
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


        //[Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var result = await _service.GetPendingCasesAsync();
            return Ok(result);
        }


        //[Authorize(Roles = "Verified,Admin")]
        [HttpPost]
        //[Consumes("multipart/form-data")]
        public async Task<IActionResult> Create( CreateLongTermCaseDto dto)
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

            // If a regular user edits the case, it goes back to Pending until an Admin approves it again
            await _service.UpdateAsync(id, dto, CurrentUserId, IsAdmin);
            return NoContent();
        }


        //[Authorize]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("User identity not found.");

            // Soft delete: hidden from the owner/public, Admin still sees it under /deleted
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
