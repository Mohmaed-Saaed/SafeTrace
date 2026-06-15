using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.LongTermCases;
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

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool IsAdmin => User.IsInRole("Admin");

        /// <summary>Public list with search/filter/sort/pagination (FR-21 .. FR-25).</summary>
        // GET: api/LongTermMissingCases?name=...&gender=...&ageCategory=...&sortDescending=true&pageNumber=1&pageSize=12
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] LongTermCaseFilterDto filter)
        {
            var result = await _service.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Cases marked as Found - Founded Cases section (FR-50 .. FR-53).</summary>
        // GET: api/LongTermMissingCases/founded
        [HttpGet("founded")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFounded()
        {
            var result = await _service.GetFoundedCasesAsync();
            return Ok(result);
        }

        /// <summary>Full case details.</summary>
        // GET: api/LongTermMissingCases/5
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>Cases reported by the current user ("My Cases").</summary>
        // GET: api/LongTermMissingCases/my-cases
        [Authorize]
        [HttpGet("my-cases")]
        public async Task<IActionResult> GetMyCases()
        {
            if (CurrentUserId == null) return Unauthorized();

            var result = await _service.GetMyCasesAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>Admin queue of cases awaiting approval (FR-20).</summary>
        // GET: api/LongTermMissingCases/pending
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var result = await _service.GetPendingCasesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new Long-Term Missing Case (FR-18 .. FR-20).
        /// Only Verified Users / Admins may create one (BR-2).
        /// The case is created with Status = Pending until an Admin approves it.
        /// </summary>
        // POST: api/LongTermMissingCases  (multipart/form-data)
        [Authorize(Roles = "Verified,Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) return Unauthorized();

            var id = await _service.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>Updates a case. Only the owner or an Admin may update it.</summary>
        // PUT: api/LongTermMissingCases/5  (multipart/form-data)
        [Authorize]
        [HttpPut("{id:long}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(long id, [FromForm] UpdateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) return Unauthorized();

            var success = await _service.UpdateAsync(id, dto, CurrentUserId, IsAdmin);
            if (!success) return Forbid();
            return NoContent();
        }

        /// <summary>Deletes a case. Only the owner or an Admin may delete it.</summary>
        // DELETE: api/LongTermMissingCases/5
        [Authorize]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (CurrentUserId == null) return Unauthorized();

            var success = await _service.DeleteAsync(id, CurrentUserId, IsAdmin);
            if (!success) return Forbid();
            return NoContent();
        }

        /// <summary>Admin approval - Pending -> Active (FR-20, FR-21).</summary>
        // PUT: api/LongTermMissingCases/5/approve
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id)
        {
            var success = await _service.ApproveAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        /// <summary>Admin rejection - Pending -> Closed.</summary>
        // PUT: api/LongTermMissingCases/5/reject
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id)
        {
            var success = await _service.RejectAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        /// <summary>Mark as Founded - Active -> Found, creates FoundPersonInfo (FR-47 .. FR-50).</summary>
        // PUT: api/LongTermMissingCases/5/mark-as-founded
        [Authorize]
        [HttpPut("{id:long}/mark-as-founded")]
        public async Task<IActionResult> MarkAsFounded(long id, [FromBody] MarkAsFoundedDto dto)
        {
            if (CurrentUserId == null) return Unauthorized();

            var success = await _service.MarkAsFoundedAsync(id, dto, CurrentUserId, IsAdmin);
            if (!success) return BadRequest();
            return NoContent();
        }
    }
}
