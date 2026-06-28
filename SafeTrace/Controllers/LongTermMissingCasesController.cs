using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.LongTermCases;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Authorization;
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

        // ─────────────────────────────────────────────────────────────
        // PUBLIC ENDPOINTS (AllowAnonymous)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// جلب الحالات النشطة مع الفلترة والـ pagination — متاح للجميع
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] LongTermCaseFilterDto filter)
        {
            var result = await _service.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// جلب الحالات التي تم إيجادها — متاح للجميع
        /// </summary>
        [HttpGet("founded")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFounded()
        {
            var result = await _service.GetFoundedCasesAsync();
            return Ok(result);
        }

        /// <summary>
        /// جلب حالة بالـ ID — متاح للجميع (الأدمن يشوف المحذوفة أيضًا)
        /// </summary>
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id, includeDeleted: IsAdmin);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────
        // USER ENDPOINTS (Authenticated)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// جلب حالات المستخدم الحالي — يتطلب تسجيل الدخول
        /// </summary>
        [HttpGet("my-cases")]
        [HasPermission(Permissions.LongTermCases.GetMyCases)]
        public async Task<IActionResult> GetMyCases()
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            var result = await _service.GetMyCasesAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>
        /// إنشاء حالة جديدة — يتطلب صلاحية Create
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.LongTermCases.Create)]
        public async Task<IActionResult> Create([FromForm] CreateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            var id = await _service.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>
        /// تعديل حالة — المستخدم يعدل حالته، الأدمن يعدل أي حالة
        /// </summary>
        [HttpPut("{id:long}")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.LongTermCases.Update)]
        public async Task<IActionResult> Update(long id, [FromForm] UpdateLongTermCaseDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            await _service.UpdateAsync(id, dto, CurrentUserId, IsAdmin);
            return NoContent();
        }

        /// <summary>
        /// حذف مؤقت — المستخدم يحذف حالته، الأدمن يحذف أي حالة
        /// </summary>
        [HttpDelete("{id:long}")]
        [HasPermission(Permissions.LongTermCases.SoftDelete)]
        public async Task<IActionResult> Delete(long id)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            await _service.DeleteAsync(id, CurrentUserId, IsAdmin);
            return NoContent();
        }

        /// <summary>
        /// تحديد الحالة كـ "تم إيجاده" — المستخدم صاحب الحالة أو الأدمن
        /// </summary>
        [HttpPut("{id:long}/mark-as-founded")]
        [HasPermission(Permissions.LongTermCases.MarkAsFounded)]
        public async Task<IActionResult> MarkAsFounded(long id, [FromBody] MarkAsFoundedDto dto)
        {
            if (CurrentUserId == null) throw new UnauthorizedException("لم يتم التعرف على هوية المستخدم.");

            await _service.MarkAsFoundedAsync(id, dto, CurrentUserId, IsAdmin);
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────────
        // ADMIN ENDPOINTS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// جلب الحالات للأدمن — فلتر اختياري بالحالة (بدون فلتر = كل الحالات)
        /// </summary>
        [HttpGet("admin-cases")]
        [HasPermission(Permissions.LongTermCases.GetAll)]
        public async Task<IActionResult> GetAdminCases([FromQuery] CaseStatus? status)
        {
            var result = await _service.GetAdminCasesAsync(status);
            return Ok(result);
        }

        /// <summary>
        /// حذف نهائي — للأدمن فقط، يشتغل على الحالات المحذوفة مؤقتًا بس
        /// </summary>
        [HttpDelete("{id:long}/permanent")]
        [HasPermission(Permissions.LongTermCases.HardDelete)]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            await _service.PermanentDeleteAsync(id);
            return NoContent();
        }

        /// <summary>
        /// الموافقة على حالة — للأدمن فقط
        /// </summary>
        [HttpPut("{id:long}/approve")]
        [HasPermission(Permissions.LongTermCases.Approve)]
        public async Task<IActionResult> Approve(long id)
        {
            await _service.ApproveAsync(id);
            return NoContent();
        }

        /// <summary>
        /// رفض حالة — للأدمن فقط
        /// </summary>
        [HttpPut("{id:long}/reject")]
        [HasPermission(Permissions.LongTermCases.Reject)]
        public async Task<IActionResult> Reject(long id)
        {
            await _service.RejectAsync(id);
            return NoContent();
        }
    }
}