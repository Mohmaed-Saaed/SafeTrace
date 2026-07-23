using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Complaints;
using SafeTrace.Application.DTOs.Complaints.Request;
using SafeTrace.Application.DTOs.Complaints.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;
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
         [HasPermission(Permissions.Complaints.GetAll)] //--------------------------->permission check is commented out for testing purposes. Remove the comment in production.
        public async Task<IActionResult> GetAll([FromQuery] ComplaintFilterDto filter)
        {
            var result = await _complaintService.GetAllAsync(filter);
            return Ok(ApiResponse<PaginationResponseDto<ComplaintResponseDto>>.Ok(result));
        }

        /// <summary>
        /// استرجاع إحصائيات الشكاوى (الإجمالي، المحلولة، والمعلقة).
        /// </summary>
        /// <response code="200">تم جلب إحصائيات الشكاوى بنجاح.</response>
        /// <response code="401">غير مسجل الدخول.</response>
        /// <response code="403">ليس لديك صلاحية لعرض الإحصائيات.</response>
        [ProducesResponseType(typeof(ApiResponse<ComplaintStatisticsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet("statistics")]
        [HasPermission(Permissions.Complaints.GetComplaintsStatistics)]
        public async Task<IActionResult> GetStatistics()
        {
            var result = await _complaintService.GetStatisticsAsync();
            return Ok(ApiResponse<ComplaintStatisticsDto>.Ok(result, "تم جلب احصائيات الشكاوى بنجاح."));
        }

        // GET /api/complaints/5
        /// <summary>
        /// Get a single complaint by ID.
        /// </summary>
        /// <remarks>
        /// Returns complaint details including user email, message, solution message, and status.
        /// </remarks>
        [HttpGet("{id:long}")]
         [HasPermission(Permissions.Complaints.GetById)] //--------------------------->permission check is commented out for testing purposes. Remove the comment in production.

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
        [Authorize] //--------------------------->permission check is commented out for testing purposes. Remove the comment in production.
        [EnableRateLimiting("ComplaintLimit")]
        public async Task<IActionResult> Create([FromBody] CreateComplaintDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("تعذر التحقق من هوية المستخدم.");
            }

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
         [HasPermission(Permissions.Complaints.MarkAsSolved)] //--------------------------->permission check is commented out for testing purposes. Remove the comment in production.

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
         [HasPermission(Permissions.Complaints.HardDelete)] //--------------------------->permission check is commented out for testing purposes. Remove the comment in production.

        public async Task<IActionResult> Delete(long id)
        {
            await _complaintService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok(message: "Complaint deleted successfully."));
        }
    }
}