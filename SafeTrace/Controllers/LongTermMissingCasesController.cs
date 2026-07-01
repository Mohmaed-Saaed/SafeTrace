using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.Exceptions;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.DTOs.LongTermCases.Request;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LongTermMissingCasesController : ControllerBase
    {
        private readonly ILongTermCaseService _service;

        public LongTermMissingCasesController(ILongTermCaseService service, ICasesService casesService)
        {
            _service = service;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

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
            // return CreatedAtAction(nameof(GetById), new { id }, new { id });
            return Ok();
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
    }
}