using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.UrgentCase.Request;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Infrastructure.Authorization;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrgentCaseController : ControllerBase
    {
        private readonly IUrgentCaseService _urgentCaseService;
        private readonly ICasesService _casesService;

        public UrgentCaseController(IUrgentCaseService urgentCaseService, ICasesService casesService)
        {
            _urgentCaseService = urgentCaseService;
            _casesService = casesService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.UrgentCases.Create)]
        public async Task<IActionResult> Create([FromForm] UrgentCaseCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.CreateAsync(userId, dto));
        }

        [HttpPut]
        [HasPermission(Permissions.UrgentCases.Update)]
        public async Task<IActionResult> Update([FromForm] UrgentCaseUpdateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("User identity could not be verified from token.");

            return Ok(await _urgentCaseService.UpdateAsync(userId, dto));
        }
    }
}