using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Request;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Authorization;

namespace SafeTrace.API.Controllers
{
    [ApiController]
    [Route("api/facebook-posts")]
    public class FacebookPostsController : ControllerBase
    {
        private readonly IFacebookImportedPostService _facebookPostService;

        public FacebookPostsController(
            IFacebookImportedPostService facebookPostService)
        {
            _facebookPostService = facebookPostService;
        }

        [HttpGet]
        // [HasPermission(Permissions.FacebookPosts.GetAll)]
        public async Task<IActionResult> GetAll([FromQuery] FacebookPostFilterDto filter)
        {
            var result = await _facebookPostService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        // [HasPermission(Permissions.FacebookPosts.GetById)]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _facebookPostService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPut("{id:long}")]
        // [HasPermission(Permissions.FacebookPosts.Update)]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateFacebookImportedPostDto dto)
        {
            var result = await _facebookPostService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpPost("{id:long}/reject")]
        // [HasPermission(Permissions.FacebookPosts.Reject)]
        public async Task<IActionResult> Reject(long id)
        {
            var result = await _facebookPostService.RejectAsync(id);
            return Ok(result);
        }

        [HttpPost("{id:long}/publish")]
        // [HasPermission(Permissions.FacebookPosts.Publish)]
        public async Task<IActionResult> Publish(
            long id,
            [FromBody] PublishFacebookImportedPostRequestDto? dto = null)
        {
            var result = await _facebookPostService.PublishAsync(id, dto);
            return Ok(result);
        }
    }
}
