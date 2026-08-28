using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices.IFacebookIntegration;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Authorization;

namespace SafeTrace.API.Controllers
{
    [ApiController]
    [Route("api/facebook-pages")]
    public class FacebookPagesController : ControllerBase
    {
        private readonly IFacebookPageService _facebookPageService;

        public FacebookPagesController(IFacebookPageService facebookPageService)
        {
            _facebookPageService = facebookPageService;
        }

        [HttpPost("integrate")]
        // [HasPermission(Permissions.FacebookPages.Create)]
        public async Task<IActionResult> Integrate([FromBody] IntegrateFacebookPageDto dto)
        {
            var result = await _facebookPageService.IntegrateAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        // [HasPermission(Permissions.FacebookPages.GetAll)]
        public async Task<IActionResult> GetAll([FromQuery] FacebookPageFilterDto? filter = null)
        {
            var result = await _facebookPageService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpPost("{id:long}/disconnect")]
        // [HasPermission(Permissions.FacebookPages.Disconnect)]
        public async Task<IActionResult> Disconnect(long id)
        {
            var result = await _facebookPageService.DisconnectAsync(id);
            return Ok(result);
        }

        [HttpPost("{id:long}/reconnect")]
        // [HasPermission(Permissions.FacebookPages.Connect)]
        public async Task<IActionResult> Reconnect(long id, [FromBody] ReconnectFacebookPageDto dto)
        {
            var result = await _facebookPageService.ReconnectAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id:long}")]
        // [HasPermission(Permissions.FacebookPages.Disconnect)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _facebookPageService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
