using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Founded.Request;
using SafeTrace.Application.Interfaces;


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

        /// <summary>
        /// Retrieves a paginated list of found persons.
        /// </summary>
        /// <param name="query">
        /// Filtering and pagination options such as search text, gender, age category, page number, and page size.
        /// </param>
        /// <returns>A paginated list of found person records.</returns>
        /// <response code="200">Found persons retrieved successfully.</response>
        /// <response code="400">The request parameters are invalid.</response>

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index([FromQuery] FoundedHeaderQueryDTO query)
        {
            var response = await _foundedService.GetAllAsync(query);
            return Ok(response);
        }
        /// <summary>
        /// Retrieves the details of a specific found person.
        /// </summary>
        /// <param name="id">The unique identifier of the found person.</param>
        /// <returns>Detailed information about the selected found person.</returns>
        /// <response code="200">Found person details retrieved successfully.</response>
        /// <response code="404">No found person exists with the specified ID.</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> PostDetail(long id)
        {
            var response = await _foundedService.GetDetailsAsync(id);
            return Ok(response);
        }
    }
}