using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Founded;
using SafeTrace.Application.DTOs.Founded.Request;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces;
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
        public async Task<IActionResult> Index([FromQuery] FoundedHeaderQueryDTO query)
        {
            var response = await _foundedService.GetAllAsync(query);
            return Ok(response);
        }

        [HttpGet("{id}")]

        public async Task<IActionResult> PostDetail(long id)
        {
            var response = await _foundedService.GetDetailsAsync(id);
            return Ok(response);
        }


    }
}
