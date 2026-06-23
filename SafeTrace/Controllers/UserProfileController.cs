using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
namespace SafeTrace.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _user;
        public UserProfileController(IUserProfileService userProfileService)
        {
            _user = userProfileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var profile = await _user.GetProfileInfoAsync(userId);
            return Ok(profile);

        }

        [HttpPut("UpdateInfo")]
        public async Task<IActionResult> UpdateUserInfo(UpdateProfileInfoDTO dTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var UpdateProfile = await _user.UpdateProfileInfoAsync(userId, dTO);
            if (UpdateProfile == null)
                return NotFound();
            return NoContent();
        }

        #region  Test Before Jwt


        [HttpGet("TestAll")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _user.GetAllUsersAsync();
                return Ok(users);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("test/{id}")]
        public async Task<IActionResult> UpdateTest(string id, UpdateProfileInfoDTO dto)
        {

            var result = await _user.UpdateProfileInfoAsync(id, dto);

            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpGet("test Claims")]
        public IActionResult Test()
        {
            return Ok(new
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Claims = User.Claims.Select(c => new
                {
                    c.Type,
                    c.Value
                })
            });
        }
        [HttpGet("test specifc user")]
        public async Task<IActionResult> GetUserInfoTest(string userId)
        {
            var profile = await _user.GetProfileInfoAsync(userId);
            return Ok(profile);
        }
        #endregion
    }
}
