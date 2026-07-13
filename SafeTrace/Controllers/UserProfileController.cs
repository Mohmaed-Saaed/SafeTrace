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
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var profile = await _user.GetProfileInfoAsync(GetCurrentUserId());
            return Ok(profile);
        }

        #region  Update
        /// <summary>
        /// لتغيير اسم المستخدم 
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateName")]
        [HasPermission(Permissions.Profile.UpdateName)]
        public async Task<IActionResult> UpdateName([FromForm] UpdateNameDTO dTO)
        {
            var UpdateProfile = await _user.UpdateNameAsync(GetCurrentUserId(), dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }

        /// <summary>
        /// تغيير صورة الملف الشخصي
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateProfileImage")]
        [HasPermission(Permissions.Profile.UpdateProfileImage)]
        public async Task<IActionResult> UpdateProfileImage([FromForm] UpdateProfileImageDTO dTO)
        {
            var UpdateProfile = await _user.UpdateProfilImageesync(GetCurrentUserId(), dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }

        /// <summary>
        /// حذف صورة الملف الشخصي   
        /// </summary>
        /// <returns></returns>
        [HttpDelete("ProfileImage")]
        [HasPermission(Permissions.Profile.UpdateProfileImage)]
        public async Task<IActionResult> RemoveProfileImage()
        {

            var result = await _user.RemoveProfileImageAsync(GetCurrentUserId());

            return Ok(result);
        }
        /// <summary>
        /// اضافة صورة البطاقة الشخصية
        /// </summary>
        /// <returns></returns>
        [HttpPut("AddIdImage")]
        [HasPermission(Permissions.Profile.UpdateIdImage)]
        public async Task<IActionResult> AddIdImage([FromForm] AddIdImageDTO dTO)
        {
            var UpdateProfile = await _user.AddIdImageAsync(GetCurrentUserId(), dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }
        /// <summary>
        /// تعديل محل الاقامة او موقع المنزل
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateHomeLocation")]
        [HasPermission(Permissions.Profile.UpdateHomeLocation)]
        public async Task<IActionResult> UpdateHomeLocation([FromForm] UpdateHomeLocationDTO dTO)
        {
            var UpdateProfile = await _user.UpdateHomeLocationAsync(GetCurrentUserId(), dTO);
            if (UpdateProfile == null)
                return NotFound();
            return Ok(UpdateProfile);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("تعذر التحقق من هوية المستخدم.");
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
