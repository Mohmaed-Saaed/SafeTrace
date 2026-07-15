using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;

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
        [HasPermission(Permissions.Dashboard.GetStatistics)]
        public async Task<IActionResult> GetDashboardData()
        {
           var response = await _dashboardService.GetDashboardAsync();
            return Ok(response);
        }
    }
}
