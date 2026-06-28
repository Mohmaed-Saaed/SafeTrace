using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.UnKnownDtos;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Services;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Authorization;
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
        [HttpPost]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.UnknownCases.Create)]
        public async Task<IActionResult> CreateUnknown([FromForm] CreateUnknownDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedException("User is not authenticated.");

            var result = await _unKnownServiceCase.CreateUnknownCaseAsync(dto, userId);

            return Ok(result);
        }
        //Test-Create-EndPoint

        //[HttpPost]
        //public async Task<IActionResult> CreateUnknownCase(
        //    [FromForm] CreateUnknownDto dto,
        //   [FromQuery] string userId)
        //{
        //    var result = await _unKnownServiceCase
        //        .CreateUnknownCaseAsync(dto, userId);

        //    return Ok(result);
        //}


        [HasPermission(Permissions.UnknownCases.Approve)]
        [HttpPut("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id)
        {
            var response = await _unKnownServiceCase.ApproveAsync(id);
            return Ok(response);
        }
        [HasPermission(Permissions.UnknownCases.Reject)]
        [HttpPut("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id)
        {
            var response = await _unKnownServiceCase.RejectAsync(id);
            return Ok(response);
        }
        [HttpGet("GetAllapproved")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllApproved(
        int pageNumber = 1,
        int pageSize = 10)
        {
            var result = await _unKnownServiceCase
                .GetAllApprovedAsync(pageNumber, pageSize);

            return Ok(result);
        }

        //[HttpGet("Filter")]
        //public async Task<IActionResult> GetCases([FromQuery] UnKnownCaseFilterDto filter)
        //{
        //    var result = await _unKnownServiceCase.GetCasesAsync(filter);
        //    return Ok(result);
        //}

        
        [HttpPut("{id}/UpdateUnKnownCase")]
        [HasPermission(Permissions.UnknownCases.Update)]
        public async Task<IActionResult> UpdateUnknownCase(long id, [FromForm] UpdateUnkownCaseDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _unKnownServiceCase
                .UpdateUnknownCaseAsync(id, dto, userId);

            return Ok(result);
        }
        //testing 
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateUnknownCase(long id,[FromForm] UpdateUnkownCaseDto dto,[FromQuery] string userId)
        //{
        //    var result = await _unKnownServiceCase
        //        .UpdateUnknownCaseAsync(id, dto, userId);

        //    return Ok(result);
        //}

        [HttpGet("{id}/GetDetails")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetails(long id)
        {
            var result = await _unKnownServiceCase.GetDetailsAsync(id);
            return Ok(result);
        }

        [HttpDelete("{id}/SoftDelete")]

        [HasPermission(Permissions.UnknownCases.SoftDelete)]
        public async Task<IActionResult> DeleteUnknownCase(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _unKnownServiceCase.DeleteUnKnownCase(id, userId);

            return Ok(result);
        }
        //testing 
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteUnknownCase(long id ,[FromQuery] string userId)
        //{
        //    var result = await _unKnownServiceCase.DeleteUnKnownCase(id, userId);

        //    return Ok(result);
        //}

        [HttpPut("{id}/UpdateTobeFound")]
        [HasPermission(Permissions.UnknownCases.MarkAsFounded)]
        public async Task<IActionResult> FoundUnknownCase(long id, [FromQuery] string userId)
        {
            var result = await _unKnownServiceCase.FoundUnKnownCase(id, userId);

            return Ok(result);
        }

        [HttpGet("status/{status}")]
        [HasPermission(Permissions.UnknownCases.GetAll)]
        public async Task<IActionResult> GetByStatus(
        CaseStatus status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {
            var filter = new UnKnownCaseFilterStatusDto
            {
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _unKnownServiceCase
                .GetAllWithFilteration(filter);

            return Ok(result);
        }

        [HttpGet("my-cases")]
        [HasPermission(Permissions.UnknownCases.GetMyCases)]
        public async Task<IActionResult> GetMyCases()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _unKnownServiceCase.GetMyCasesAsync(userId!);

            return Ok(result);
        }
        //[HttpGet("my-cases")]
        //public async Task<IActionResult> GetMyCases([FromQuery] string userId)
        //{
        //    var result = await _unKnownServiceCase
        //        .GetMyCasesAsync(userId);

        //    return Ok(result);
        //}

        [HasPermission(Permissions.UnknownCases.HardDelete)]
        [HttpDelete("hard-delete/{id}")]
        public async Task<IActionResult> HardDelete(long id)
        {
            var result = await _unKnownServiceCase.HardDeleteUnknownCase(id);
            return Ok(result);
        }
        [HttpGet("GetCasebyFilteration")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCases(
             [FromQuery] UnknownFilterUsingbyUserDto filter)
        {
            var result = await _unKnownServiceCase
               .GetCasesAsync(filter);

            return Ok(result);
        }

    }
}
