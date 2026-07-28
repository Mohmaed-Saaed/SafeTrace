using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Dashboard.Request;
using SafeTrace.Application.DTOs.Dashboard.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers.Dashboard
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        public DashboardController(IDashboardService dashboardService)  
        {
            _dashboardService = dashboardService;
            
        }
        /// <summary>
        /// Retrieves dashboard statistics and summary information.
        /// </summary>
        /// <remarks>
        /// Returns aggregated statistics including:
        /// - Total registered users
        /// - Total missing person cases
        /// - Total active cases
        /// - Total pending cases
        /// - Total found cases
        /// - Total deleted cases
        /// - Case statistics grouped by case type
        /// </remarks>
        /// <returns>A dashboard summary containing overall system statistics.</returns>
        /// <response code="200">Dashboard data retrieved successfully.</response>
        /// <response code="500">An unexpected error occurred while retrieving dashboard data.</response>
        [HttpGet]
        //[HasPermission(Permissions.Dashboard.GetStatistics)]
        public async Task<IActionResult> GetDashboardData()
        {
           var response = await _dashboardService.GetDashboardAsync();
            return Ok(response);
        }

        /// <summary>
        /// استرجاع إحصائيات الحالات (الإجمالي، المفتوحة، المغلقة، الخ).
        /// </summary>
        /// <response code="200">تم جلب إحصائيات الحالات بنجاح.</response>
        /// <response code="401">غير مسجل الدخول.</response>
        /// <response code="403">ليس لديك صلاحية لعرض الإحصائيات.</response>
        [ProducesResponseType(typeof(ApiResponse<CasesStatisticsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet("cases-statistics")]
        [HasPermission(Permissions.Dashboard.GetCasesStatistics)]
        public async Task<IActionResult> GetCasesStatistics()
        {
            var response = await _dashboardService.GetCasesStatisticsAsync();
            return Ok(response);
        }

        /// <summary>
        /// استرجاع سجلات النظام (Audit Logs). متاح فقط للمدير الأساسي.
        /// </summary>
        [ProducesResponseType(typeof(ApiResponse<PaginationResponseDto<AuditLogDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HasPermission(Permissions.Dashboard.GetAuditLogs)]
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryDto query)
        {
            var response = await _dashboardService.GetAuditLogsAsync(query);
            return Ok(response);
        }

        [HttpPost("cases/report/pdf")]
        public async Task<IActionResult> GenerateCasesPdfReport(
            [FromBody] CasesReportFilterDto filter)
        {
            var file = await _dashboardService
                .GeneratePdfReportAsync(filter);


            return File(
                file,
                "application/pdf",
                $"Cases_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }
    }
}
