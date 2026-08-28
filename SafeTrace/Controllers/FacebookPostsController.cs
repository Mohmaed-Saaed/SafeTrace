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
            return Ok(ApiResponse<List<FacebookImportedPostListDto>>.Ok(result));
        }

        [HttpGet("{id:long}")]
        // [HasPermission(Permissions.FacebookPosts.GetById)]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _facebookPostService.GetByIdAsync(id);
            return Ok(ApiResponse<FacebookImportedPostDetailDto>.Ok(result));
        }

        [HttpPut("{id:long}")]
        // [HasPermission(Permissions.FacebookPosts.Update)]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateFacebookImportedPostDto dto)
        {
            var result = await _facebookPostService.UpdateAsync(id, dto);
            return Ok(ApiResponse<FacebookImportedPostDetailDto>.Ok(result));
        }

        [HttpPost("{id:long}/reject")]
        // [HasPermission(Permissions.FacebookPosts.Reject)]
        public async Task<IActionResult> Reject(long id)
        {
            var result = await _facebookPostService.RejectAsync(id);
            return Ok(ApiResponse<FacebookImportedPostDetailDto>.Ok(
                result,
                "Facebook imported post rejected successfully."));
        }

        [HttpPost("{id:long}/publish")]
        // [HasPermission(Permissions.FacebookPosts.Publish)]
        public async Task<IActionResult> Publish(
            long id,
            [FromBody] PublishFacebookImportedPostRequestDto? dto = null)
        {
            var result = await _facebookPostService.PublishAsync(id, dto);
            var message = result.Status == FacebookImportedPostStatus.Duplicate
                ? (result.Message ?? "This case already exists in the system.")
                : "Facebook imported post published successfully.";

            return Ok(ApiResponse<PublishFacebookImportedPostResponseDto>.Ok(
                result,
                message));
        }
    }
}
