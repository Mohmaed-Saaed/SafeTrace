using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Exceptions;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IEmailServiceSendGrid _emailServiceSendGrid;
        public AuthController(IEmailServiceSendGrid emailServiceSendGrid)
        {
          _emailServiceSendGrid = emailServiceSendGrid;   
        }

        [HttpGet]
        public async Task<IActionResult> test()
        {
            throw new NotFoundException("Saalllaaaammmmm");
            await _emailServiceSendGrid.SendEmailAsync("jcdk", "dkjcndj", "srhgfbd");
            return Ok();
        }
    }
}