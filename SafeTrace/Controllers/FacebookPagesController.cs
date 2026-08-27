using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;
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

        [HttpPost]
        [HasPermission(Permissions.FacebookPages.Create)]
        public async Task<IActionResult> Create([FromBody] CreateFacebookPageDto dto)
        {
            var result = await _facebookPageService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                ApiResponse<FacebookPageResponseDto>.Ok(
                    result,
                    "Facebook page created successfully."));
        }

        [HttpPut("{id:long}")]
        [HasPermission(Permissions.FacebookPages.Update)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateFacebookPageDto dto)
        {
            var result = await _facebookPageService.UpdateAsync(id, dto);
            return Ok(ApiResponse<FacebookPageResponseDto>.Ok(result));
        }

        [HttpGet]
        [HasPermission(Permissions.FacebookPages.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _facebookPageService.GetAllAsync();
            return Ok(ApiResponse<List<FacebookPageResponseDto>>.Ok(result));
        }

        [HttpGet("{id:long}")]
        [HasPermission(Permissions.FacebookPages.GetById)]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _facebookPageService.GetByIdAsync(id);
            return Ok(ApiResponse<FacebookPageResponseDto>.Ok(result));
        }

        [HttpPost("{id:long}/connect")]
        [HasPermission(Permissions.FacebookPages.Connect)]
        public async Task<IActionResult> Connect(long id)
        {
            var result = await _facebookPageService.ConnectAsync(id);
            return Ok(ApiResponse<FacebookPageResponseDto>.Ok(result));
        }

        [HttpPost("{id:long}/disconnect")]
        [HasPermission(Permissions.FacebookPages.Disconnect)]
        public async Task<IActionResult> Disconnect(long id)
        {
            var result = await _facebookPageService.DisconnectAsync(id);
            return Ok(ApiResponse<FacebookPageResponseDto>.Ok(result));
        }
    }
}
