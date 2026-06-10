using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Founded;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.Services;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Enums;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoundedController : ControllerBase
    {

        private readonly IFoundedService _foundedService;
        public FoundedController(IFoundedService foundedService)
        {
            _foundedService = foundedService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, Gender? gender, int page = 1, int pageSize = 10)
        {
            var response = await _foundedService.GetAllAsync(search, gender, page, pageSize);
            return Ok(response);
        }

    }
}
