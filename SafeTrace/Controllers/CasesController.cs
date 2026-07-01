using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.SharedCases;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CasesController : ControllerBase
    {
        private readonly ICasesService _casesService;

        public CasesController(ICasesService casesService)
        {
            _casesService = casesService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] CasesFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _casesService.GetCasesAsync(filter, userId: userId));
        }

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            return Ok(await _casesService.GetCaseByIdAsync(id));
        }

        [HttpGet("my-cases")]
        public async Task<IActionResult> GetMyCases([FromQuery] CasesFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) 
                throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _casesService.GetMyCasesAsync(userId, filter));
        }

        [HttpGet("admin")]
        public async Task<IActionResult> AdminGetAll([FromQuery] CasesFilterDto filter)
        {
            return Ok(await _casesService.AdminGetCasesAsync(filter));
        }

        [HttpPut("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id)
        {
            await _casesService.ApproveAsync(id);
            return NoContent();
        }

        [HttpPut("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id)
        {
            await _casesService.RejectAsync(id);
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> SoftDelete(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) 
                throw new UnauthorizedException("User identity could not be verified from token.");

            var isAdmin = User.IsInRole("Admin");
            await _casesService.SoftDeleteAsync(id, userId, isAdmin);
            return NoContent();
        }

        [HttpPut("{id:long}/mark-as-found")]
        public async Task<IActionResult> MarkAsFound(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) 
                throw new UnauthorizedException("User identity could not be verified from token.");

            var isAdmin = User.IsInRole("Admin");
            await _casesService.MarkAsFoundAsync(id, userId, isAdmin: isAdmin);
            return NoContent();
        }

        [HttpDelete("{id:long}/permanent")]
        public async Task<IActionResult> PermanentDelete(long id)
        {
            await _casesService.PermanentDeleteAsync(id);
            return NoContent();
        }
    }
}