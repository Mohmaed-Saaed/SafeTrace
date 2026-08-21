using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;
namespace SafeTrace.API.Controllers
{
    public class UserProfileController : BaseApiController
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
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var profile = await _user.GetProfileInfoAsync(CurrentUserId);
            return Ok(profile);
        }
        /// <summary>
        /// الحصول على بيانات الملف الشخصي لمستخدم عن طريق معرفه.
        /// </summary>
        /// <param name="Id">معرف المستخدم.</param>
        /// <returns>بيانات الملف الشخصي للمستخدم.</returns>
        [HttpGet("GetVisitedUserInfo/{Id}")]
        [AllowAnonymous]
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
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ProfileUpdateLimit)]
        public async Task<IActionResult> UpdateName([FromForm] UpdateNameDTO dTO)
        {
            var result = await _user.UpdateNameAsync(CurrentUserId, dTO);

            return Ok(result);
        }

        /// <summary>
        /// تغيير صورة الملف الشخصي
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateProfileImage")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ProfileUpdateLimit)]
        public async Task<IActionResult> UpdateProfileImage([FromForm] UpdateProfileImageDTO dTO)
        {
            var result = await _user.UpdateProfilImageesync(CurrentUserId, dTO);
            return Ok(result);
        }

        /// <summary>
        /// حذف صورة الملف الشخصي   
        /// </summary>
        /// <returns></returns>
        [HttpDelete("ProfileImage")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ProfileUpdateLimit)]
        public async Task<IActionResult> RemoveProfileImage()
        {

            var result = await _user.RemoveProfileImageAsync(CurrentUserId);

            return Ok(result);
        }
        /// <summary>
        /// اضافة صورة البطاقة الشخصية
        /// </summary>
        /// <returns></returns>
        [HttpPut("AddIdImage")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ProfileUpdateLimit)]
        public async Task<IActionResult> AddIdImage([FromForm] AddIdImageDTO dTO)
        {
            var result = await _user.AddIdImageAsync(CurrentUserId, dTO);

            return Ok(result);
        }
        /// <summary>
        /// تعديل محل الاقامة او موقع المنزل
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateHomeLocation")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ProfileUpdateLimit)]
        public async Task<IActionResult> UpdateHomeLocation([FromForm] UpdateHomeLocationDTO dTO)
        {
            var result = await _user.UpdateHomeLocationAsync(CurrentUserId, dTO);
            return Ok(result);
        }
        /// <summary>
        /// لتحديد الموقع الحالي للمستخدم
        /// </summary>
        /// <returns></returns>
        [HttpPut("UpdateCurrentLocation")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ProfileUpdateLimit)]
        public async Task<IActionResult> UpdateCurrentLoc(UpdateCurrentLocationDTO dto)
        {
            var result = await _user.UpdateCurrentLocation(CurrentUserId, dto);
            return Ok(result);
        }

        /// <summary>
        /// تعديل رقم الهاتف
        /// </summary>
        /// <returns></returns>

        [HttpPut("UpdatePhoneNumber")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ProfileUpdateLimit)]
        public async Task<IActionResult> UpdatePhoneNumber([FromForm] ChangePhoneNumberDTO dto)
        {
            var result = await _user.UpdatePhoneNumberAsync(CurrentUserId, dto);
            return Ok(result);
        }






        #endregion

        #region My Cases
        [HttpGet("MyCases")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PaginationResponseDto<MyCaseListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyCases([FromQuery] MyCasesFilterDto filter)
        {
            var result = await _user.GetMyCasesAsync(CurrentUserId, filter);

            return Ok(result);
        }
        #endregion
    }
}
