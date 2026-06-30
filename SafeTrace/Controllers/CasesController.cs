using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Interfaces.IServices.IMissingCases;

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
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _casesService.GetCases());
        }
    }
}
