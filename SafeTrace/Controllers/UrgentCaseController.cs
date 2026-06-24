using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
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

        [HttpGet("Cases")]
        public async Task<IActionResult> GetAll([FromQuery] UrgentCaseFilterDto filter)
        {
            return Ok(await _urgentCaseService.GetAllAsync(filter));
        }

        [HttpGet("admin/Cases")]
        public async Task<IActionResult> AdminGetAll([FromQuery] UrgentCaseFilterDto filter)
        {
            return Ok(await _urgentCaseService.AdminGetAllAsync(filter));
        }

        [HttpGet("Detail")]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _urgentCaseService.GetByIdAsync(id));
        }

        [HttpGet("myCases")]
        public async Task<IActionResult> GetMyCases([FromQuery] UrgentCaseFilterDto filter)
        {
            return Ok(await _urgentCaseService.GetMyCasesAsync(filter));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UrgentCaseCreateDto dto)
        {
            return Ok(await _urgentCaseService.CreateAsync(dto));
        }

        // [HttpPut]
        // public async Task<IActionResult> Update([FromBody] UrgentCaseUpdateDto dto)
        //     => Ok(await _urgentCaseService.UpdateAsync(dto));

        // [HttpDelete("{id}")]
        // public async Task<IActionResult> Delete(long id)
        //     => Ok(await _urgentCaseService.DeleteAsync(id));

        // [HttpDelete("permanent/{id}")]
        // public async Task<IActionResult> PermanentDelete(long id)
        //     => Ok(await _urgentCaseService.PermanentDeleteAsync(id));

        // [HttpPost("{id}/approve")]
        // public async Task<IActionResult> Approve(long id)
        //     => Ok(await _urgentCaseService.ApproveAsync(id));

        // [HttpPost("{id}/reject")]
        // public async Task<IActionResult> Reject(long id)
        //     => Ok(await _urgentCaseService.RejectAsync(id));

        // [HttpPost("{id}/mark-founded")]
        // public async Task<IActionResult> MarkAsFounded(long id)
        //     => Ok(await _urgentCaseService.MarkAsFoundedAsync(id)); 
        
    }
}
