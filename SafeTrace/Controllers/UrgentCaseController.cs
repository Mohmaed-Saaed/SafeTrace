using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrgentCaseController : ControllerBase
    {
        private readonly IUrgentCaseService _urgentCaseService;

        public UrgentCaseController(IUrgentCaseService urgentCaseService)
        {
            _urgentCaseService = urgentCaseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] UrgentCaseFilterDto filter)
        {

            return Ok(await _urgentCaseService.GetAllAsync(filter));
        }

        // [HttpGet("Detail")]
        // public async Task<IActionResult> GetById(long id)
        // {
        //     return Ok(await _urgentCaseService.GetByIdAsync(id));
        // }

    }
}
