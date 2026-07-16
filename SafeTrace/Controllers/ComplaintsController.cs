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

        /// <summary>
        /// Get all complaints with pagination and optional filters.
        /// </summary>
        /// <remarks>
        /// Filter by Status: 0 = UnSolved, 1 = Solved
        /// Filter by CaseCode: optional case code string
        /// Default: PageNumber = 1, PageSize = 10
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ComplaintFilterDto filter)
        {
            var result = await _complaintService.GetAllAsync(filter);
            return Ok(ApiResponse<PaginationResponseDto<ComplaintResponseDto>>.Ok(result));
        }

        /// <summary>
        /// Get a single complaint by ID.
        /// </summary>
        /// <remarks>
        /// Returns complaint details including user email, message, solution message, and status.
        /// </remarks>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _complaintService.GetByIdAsync(id);
            return Ok(ApiResponse<ComplaintResponseDto>.Ok(result));
        }

        /// <summary>
        /// User submits a new complaint.
        /// </summary>
        /// <remarks>
        /// CaseCode is optional. Message is required.
        /// UserId is extracted automatically from the JWT token.
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateComplaintDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? "1dcc168b-80da-4909-8438-4e177016be76";
            var result = await _complaintService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<ComplaintResponseDto>.Ok(result, "Complaint created successfully."));
        }

        /// <summary>
        /// Admin resolves a complaint and notifies the user.
        /// </summary>
        /// <remarks>
        /// Updates complaint status to Solved.
        /// Sends in-app Notification to the user.
        /// Sends Email to the user with the solution message.
        /// </remarks>
        [HttpPut("{id:long}/resolve")]
        public async Task<IActionResult> Resolve(long id, [FromBody] ResolveComplaintDto dto)
        {
            await _complaintService.ResolveAsync(id, dto);
            return Ok(ApiResponse<string>.Ok(message: "Complaint resolved and user notified successfully."));
        }

        /// <summary>
        /// Delete a complaint by ID.
        /// </summary>
        /// <remarks>
        /// Permanently removes the complaint from the database.
        /// </remarks>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _complaintService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok(message: "Complaint deleted successfully."));
        }
    }
}