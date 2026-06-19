using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.UnKnownDtos;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Services;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnKnownController : ControllerBase
    {
        private readonly IUnknownCaseService _unKnownServiceCase;

        public UnKnownController(IUnknownCaseService unKnownServiceCase)
        {
            _unKnownServiceCase = unKnownServiceCase;
        }
        //[HttpPost]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> CreateUnknown([FromForm] CreateUnknownDto dto)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //        throw new UnauthorizedException("User is not authenticated.");

        //    var result = await _unKnownServiceCase.CreateUnknownCaseAsync(dto, userId);

        //    return Ok(result);
        //}
        //Test-Create-EndPoint

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateUnknown(
        [FromForm] CreateUnknownDto dto,
        [FromQuery] string userId)
        {
            var result = await _unKnownServiceCase.CreateUnknownCaseAsync(dto, userId);

            return Ok(result);
        }

        //[Authorize(Roles = "Admin")]
        [HttpPut("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id)
        {
            var response = await _unKnownServiceCase.ApproveAsync(id);
            return Ok(response);
        }
        //[Authorize(Roles = "Admin")]
        [HttpPut("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id)
        {
            var response = await _unKnownServiceCase.RejectAsync(id);
            return Ok(response);
        }
        [HttpGet("GetAllapproved")]
        public async Task<IActionResult> GetAllApproved()
        {
            var response = await _unKnownServiceCase.GetAllApprovedAsync();
            return Ok(response);
        }
        [HttpGet]
        public async Task<IActionResult> GetCases([FromQuery] UnKnownCaseFilterDto filter)
        {
            var result = await _unKnownServiceCase.GetCasesAsync(filter);
            return Ok(result);
        }





    }
}
