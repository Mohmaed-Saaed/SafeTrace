using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Application.Services.UserProfileServices;
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



        #region Get Info
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
        /// <summary>
        /// الحصول على بيانات الملف الشخصي لمستخدم عن طريق معرفه.
        /// </summary>
        /// <param name="Id">معرف المستخدم.</param>
        /// <returns>بيانات الملف الشخصي للمستخدم.</returns>
        [HttpGet("GetVisitedUserInfo/{Id}")]
        [HasPermission(Permissions.Profile.GetVisitedUserInfo)]
        public async Task<IActionResult> GetVisitedUserInfo([FromRoute] string Id)
        {
            var profile = await _user.GetVisitedUserAsync(Id);
            return Ok(profile);
        }
        #endregion

        #region  Update
        /// <summary>
        /// لتغيير اسم المستخدم 
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateName")]
        [HasPermission(Permissions.Profile.UpdateName)]
        public async Task<IActionResult> UpdateName([FromForm] UpdateNameDTO dTO)
        {
            var result = await _user.UpdateNameAsync(GetCurrentUserId(), dTO);

            return Ok(result);
        }

        /// <summary>
        /// تغيير صورة الملف الشخصي
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateProfileImage")]
        [HasPermission(Permissions.Profile.UpdateProfileImage)]
        public async Task<IActionResult> UpdateProfileImage([FromForm] UpdateProfileImageDTO dTO)
        {
            var result = await _user.UpdateProfilImageesync(GetCurrentUserId(), dTO);
            return Ok(result);
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
            var result = await _user.AddIdImageAsync(GetCurrentUserId(), dTO);

            return Ok(result);
        }
        /// <summary>
        /// تعديل محل الاقامة او موقع المنزل
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateHomeLocation")]
        [HasPermission(Permissions.Profile.UpdateHomeLocation)]
        public async Task<IActionResult> UpdateHomeLocation([FromForm] UpdateHomeLocationDTO dTO)
        {
            var result = await _user.UpdateHomeLocationAsync(GetCurrentUserId(), dTO);
            return Ok(result);
        }

        /// <summary>
        /// تعديل رقم الهاتف
        /// </summary>
        /// <returns></returns>

        [HttpPut("UpdatePhoneNumber")]
        [HasPermission(Permissions.Profile.UpdatePhoneNumber)]
        public async Task<IActionResult> UpdatePhoneNumber([FromForm] ChangePhoneNumberDTO dto)
        {
            var result = await _user.UpdatePhoneNumberAsync(GetCurrentUserId(), dto);
            return Ok(result);
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
    
        #region My Cases
        [HttpGet("MyCases")]
        [ProducesResponseType(typeof(ApiResponse<PaginationResponseDto<MyCaseListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyCases([FromQuery] MyCasesFilterDto filter)
        {
            var result = await _user.GetMyCasesAsync(GetCurrentUserId(), filter);

            return Ok(result);
        }
        #endregion
    }
}
