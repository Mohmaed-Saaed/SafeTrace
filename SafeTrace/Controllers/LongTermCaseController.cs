using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.LongTermCase.Request;
using SafeTrace.Application.DTOs.LongTermCase.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    /// <summary>
    /// Endpoints for managing long-term missing person cases.
    /// </summary>
    [Produces("application/json")]
    public class LongTermCaseController : BaseApiController
    {
        private readonly ILongTermCaseService _service;

        public LongTermCaseController(ILongTermCaseService service)
        {
            _service = service;
        }

        /// <summary>Gets a paginated list of active long-term cases.</summary>
        /// <param name="filter">Filtering, sorting and pagination options.</param>
        [HttpGet("GetCases")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PaginationResponseDto<LongTermCaseListDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] LongTermCaseFilterDto filter)
        {
            return Ok(await _service.GetAllAsync(filter));
        }

        /// <summary>Gets a paginated list of all long-term cases (any status) — admin only.</summary>
        /// <param name="filter">Filtering, sorting and pagination options.</param>
        [HttpGet("Admin/GetCases")]
        [HasPermission(Permissions.Cases.GetAll)]
        [ProducesResponseType(typeof(ApiResponse<PaginationResponseDto<LongTermCaseDetailDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AdminGetAll([FromQuery] LongTermCaseFilterDto filter)
        {
            return Ok(await _service.AdminGetAllAsync(filter));
        }

        /// <summary>Gets the public details of a single active long-term case.</summary>
        /// <param name="id">Case ID.</param>
        [HttpGet("GetCaseDetails/{id:long}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LongTermCaseDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _service.GetByIdAsync(id));
        }

        /// <summary>Gets the full details of a case regardless of status — admin only.</summary>
        /// <param name="id">Case ID.</param>
        [HttpGet("Admin/GetCaseDetails/{id:long}")]
        [HasPermission(Permissions.LongTermCases.GetById)]
        [ProducesResponseType(typeof(ApiResponse<LongTermCaseDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdminGetById(long id)
        {
            return Ok(await _service.AdminGetByIdAsync(id));
        }

        /// <summary>
        /// إنشاء حالة جديدة — يتطلب صلاحية Create.
        /// قبل الإنشاء يتم التأكد من توثيق المستخدم، ثم يتم فحص الحالة الجديدة
        /// مقابل الحالات النشطة الحالية (بالوجه والبيانات):
        /// - لو فيه تطابق من نفس نوع الحالة (LongTerm) → الطلب يفشل بـ 400 (تكرار حقيقي).
        /// - لو فيه تطابق من نوع مختلف (Unknown/Urgent) → الحالة الجديدة لا يتم إنشاؤها،
        ///   ويتم إرجاع الحالة/الحالات المطابقة في MatchedCases (IsCreated = false)
        ///   حتى يتم عرض خيار محادثة صاحب الحالة على المستخدم،
        ///   أو إعادة الإرسال مع forceCreate=true لتجاهل التطابق وإنشاء الحالة بأي حال.
        /// </summary>
        /// <param name="dto">بيانات الحالة الجديدة.</param>
        /// <param name="forceCreate">لو true، يتم تجاهل أي تطابق من نوع حالة مختلف وإنشاء الحالة رغم ذلك.</param>
        [HttpPost("CreateCase")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.LongTermCases.Create)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromForm] CreateLongTermCaseDto dto, [FromQuery] bool forceCreate = false)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            return Ok(await _service.CreateAsync(CurrentUserId, dto, forceCreate));
        }

        /// <summary>
        /// تعديل حالة — المستخدم يعدل حالته، الأدمن يعدل أي حالة.
        /// </summary>
        /// <param name="id">Case ID.</param>
        /// <param name="dto">البيانات المُحدَّثة.</param>
        [HttpPut("UpdateCase/{id:long}")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.LongTermCases.Update)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(long id, [FromForm] UpdateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            await _service.UpdateAsync(id, dto);
            return NoContent();
        }

        /// <summary>Approves a pending case, changing its status to Active.</summary>
        /// <param name="id">Case ID.</param>
        [HttpPut("Approve/{id:long}")]
        [HasPermission(Permissions.LongTermCases.Approve)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(long id)
        {
            return Ok(await _service.ApproveAsync(id));
        }

        /// <summary>Rejects a pending case and records the supplied reason in the notification sent to its owner.</summary>
        /// <param name="id">Case ID.</param>
        [HttpPut("Reject/{id:long}")]
        [HasPermission(Permissions.LongTermCases.Reject)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(long id, [FromBody] string rejectionReason)
        {
            return Ok(await _service.RejectAsync(id, rejectionReason));
        }

        /// <summary>Soft-deletes a case, preserving its data for future recovery.</summary>
        /// <param name="id">Case ID.</param>
        [HttpDelete("Delete/{id:long}")]
        [HasPermission(Permissions.LongTermCases.SoftDelete)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoftDelete(long id)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            return Ok(await _service.SoftDeleteAsync(id, CurrentUserId));
        }

        /// <summary>Marks a case as found and stores the found-person information.</summary>
        /// <param name="id">Case ID.</param>
        /// <param name="foundPersonInfo">Details about how/where the person was found.</param>
        [HttpPut("MarkAsFound/{id:long}")]
        [HasPermission(Permissions.LongTermCases.MarkAsFounded)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsFound(long id, FoundPersonInfoRequestDto foundPersonInfo)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            return Ok(await _service.MarkAsFoundAsync(id, CurrentUserId, foundPersonInfo));
        }

        /// <summary>Permanently deletes a case and all its associated files/face records — irreversible.</summary>
        /// <param name="id">Case ID.</param>
        [HttpDelete("PermanentDeletion/{id:long}")]
        [HasPermission(Permissions.LongTermCases.HardDelete)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            return Ok(await _service.PermanentDeleteAsync(id));
        }

        [HttpGet("MyCaseDetails/{id:long}")]
        [Authorize]
        public async Task<IActionResult> GetMyCaseById(long id)
        {
            if (CurrentUserId == null)
                throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            return Ok(await _service.GetMyCaseByIdAsync(id, CurrentUserId));
        }
    }
}
