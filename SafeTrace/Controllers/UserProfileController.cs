using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;
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

        /// <summary>
        /// لاظهار البيانات الخاصة بالمستخدم 
        /// </summary>
        /// <returns></returns>

        [HttpGet("GetInfo")]
        [HasPermission(Permissions.Profile.GetUserInfo)]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var profile = await _user.GetProfileInfoAsync(userId);
            return Ok(profile);
        }

        #region
        /// <summary>
        /// لتغيير اسم المستخدم 
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateName")]
        [HasPermission(Permissions.Profile.UpdateUserInfo)]
        public async Task<IActionResult> UpdateName([FromForm] UpdateNameDTO dTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var UpdateProfile = await _user.UpdateNameAsync(userId, dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }

        /// <summary>
        /// لتغيير كلمة المرور 
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdatePassword")]
        [HasPermission(Permissions.Profile.UpdateUserInfo)]
        public async Task<IActionResult> UpdatePassword([FromForm] UpdatePasswordDTO dTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var UpdateProfile = await _user.UpdatePasswordAsync(userId, dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }
        /// <summary>
        /// تغيير صورة الملف الشخصي
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateProfileImage")]
        [HasPermission(Permissions.Profile.UpdateUserInfo)]
        public async Task<IActionResult> UpdateProfileImage([FromForm] UpdateProfileImageDTO dTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var UpdateProfile = await _user.UpdateProfilImageesync(userId, dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }
        /// <summary>
        /// اضافة صورة البطاقة الشخصية
        /// </summary>
        /// <returns></returns>
        [HttpPut("AddIdImage")]
        [HasPermission(Permissions.Profile.UpdateUserInfo)]
        public async Task<IActionResult> AddIdImage([FromForm] AddIdImageDTO dTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var UpdateProfile = await _user.AddIdImageAsync(userId, dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }
        /// <summary>
        /// تعديل محل الاقامة او موقع المنزل
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateHomeLocation")]
        [HasPermission(Permissions.Profile.UpdateUserInfo)]
        public async Task<IActionResult> UpdateHomeLocation([FromForm] UpdateHomeLocationDTO dTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var UpdateProfile = await _user.UpdateHomeLocationAsync(userId, dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }
        #region Old Update End Point

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
        #endregion 
        #endregion
    }
}
//https://localhost:7041/Images/Profile/61d2613d-47e1-41e1-8cd4-7340d08959ae.jpg
//https://localhost:7041/Images/Identification/5bc9ae5d-7a09-45a2-ae53-1ffa9bbf0138.png