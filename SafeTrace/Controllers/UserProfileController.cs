using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Infrastructure.Authorization;
namespace SafeTrace.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _user;
        public UserProfileController(IUserProfileService userProfileService)
        {
            _user = userProfileService;
        }

        [HttpGet("GetInfo")]
        [HasPermission(Permissions.Profile.GetUserInfo)]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var profile = await _user.GetProfileInfoAsync(userId);
            return Ok(profile);
        }

        [HttpPut("UpdateInfo")]
        [HasPermission(Permissions.Profile.UpdateUserInfo)]
        public async Task<IActionResult> UpdateUserInfo([FromForm] UpdateProfileInfoDTO dTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var UpdateProfile = await _user.UpdateProfileInfoAsync(userId, dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }

    }
}
