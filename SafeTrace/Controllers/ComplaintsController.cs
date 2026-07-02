using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Complaints;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;

        public ComplaintsController(IComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        // GET /api/complaints?pageNumber=1&pageSize=10&caseCode=ABC
        [HttpGet]

        public async Task<IActionResult> GetAll([FromQuery] ComplaintFilterDto filter)
        {
            var result = await _complaintService.GetAllAsync(filter);
            return Ok(ApiResponse<PaginationResponseDto<ComplaintResponseDto>>.Ok(result));
        }

        // GET /api/complaints/5
        [HttpGet("{id:long}")]

        public async Task<IActionResult> GetById(long id)
        {
            var result = await _complaintService.GetByIdAsync(id);
            return Ok(ApiResponse<ComplaintResponseDto>.Ok(result));
        }

        // POST /api/complaints
        [HttpPost]

        public async Task<IActionResult> Create([FromBody] CreateComplaintDto dto)
        {
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!; ---> for test
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? "1dcc168b-80da-4909-8438-4e177016be76";
            var result = await _complaintService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<ComplaintResponseDto>.Ok(result, "Complaint created successfully."));
        }

        // DELETE /api/complaints/5
        [HttpDelete("{id:long}")]
        // [Authorize] // أو Admin only
        public async Task<IActionResult> Delete(long id)
        {
            await _complaintService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok(message: "Complaint deleted successfully."));
        }
    }
}