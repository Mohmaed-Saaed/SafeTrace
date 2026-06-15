using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Interfaces.IServices;

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

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
           var response = await _dashboardService.GetDashboardAsync();
            return Ok(response);
        }
    }
}
