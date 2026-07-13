using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Application.Services.NotificationServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using static System.Net.Mime.MediaTypeNames;
using static SafeTrace.Application.Constants.Permissions;

namespace SafeTrace.Application.Services.UserProfileServices
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserProfileService> _logger;
        private readonly IFileStorageService _Image;
        private readonly INotificationServices _Notify;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public UserProfileService(UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<UserProfileService> logger,
            IFileStorageService Image,
            INotificationServices Notify,
            IHttpContextAccessor httpContextAccessor
            )
        {

            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
            _Image = Image;
            _Notify = Notify;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<ApiResponse<GetUserInfoDTO?>> GetProfileInfoAsync(string userId)
        {
            _logger.LogInformation("Fetching profile for UserId: {userId} at {Time}", userId, DateTime.UtcNow);
            //var user = await _userManager.FindByIdAsync(userId);
            var user = await _userManager.Users
    .Include(u => u.Cases)
    .FirstOrDefaultAsync(u => u.Id == userId);
            var roles = await _userManager.GetRolesAsync(user);
            if (user == null)
            {
                _logger.LogWarning("User With Id : {UserId} Not Found at {Time}", userId, DateTime.UtcNow);
                throw new NotFoundException($"المستخدم غير موجود");
            }
            else
            {
                var dto = _mapper.Map<GetUserInfoDTO>(user);
                dto.Role = roles.Contains(UserRole.Admin.ToString())
                    ? UserRole.Admin.ToString()
                    : roles.Contains(UserRole.Moderator.ToString())
                        ? UserRole.Moderator.ToString()
                        : roles.Contains(UserRole.VerifiedUser.ToString())
                            ? UserRole.VerifiedUser.ToString()
                            : UserRole.User.ToString();
                var request = _httpContextAccessor.HttpContext.Request;

                string baseUrl = $"{request.Scheme}://{request.Host}";

                dto.ProfileImage = string.IsNullOrEmpty(dto.ProfileImage)
                    ? null
                    : $"{baseUrl}{dto.ProfileImage}";

                dto.IdentificationImage = string.IsNullOrEmpty(dto.IdentificationImage)
                    ? null
                    : $"{baseUrl}{dto.IdentificationImage}";
                return ApiResponse<GetUserInfoDTO?>.Ok(dto, ".اليك بيانات المستخدم");
            }
        }


        #region Update
        public async Task<ApiResponse<bool>> AddIdImageAsync(string userId, AddIdImageDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (dto.IdentificationImage is not null)
            {
                if (user.VerificationStatus == VerificationStatus.Verified)
                {
                    throw new BadRequestException("صورة البطاقة موجودة بالفعل .");
                }
                var newIdImage =
                 await _Image.SaveFileAsync(dto.IdentificationImage, "Identification");
                if (!string.IsNullOrEmpty(user.IdentificationImage))
                {
                    _Image.DeleteFile(user.IdentificationImage);
                }

                user.IdentificationImage = newIdImage;
                user.VerificationStatus = VerificationStatus.Pending;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return ApiResponse<bool>.Ok(true, "فشل اضافة صورة بطاقة او تم اضافتها من قبل ");
                }
            }

            return ApiResponse<bool>.Ok(true, "تم اضافة صورة البطاقة بنجاح");

        }

        public async Task<ApiResponse<bool>> UpdateHomeLocationAsync(string userId, UpdateHomeLocationDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("المستخدم غير موجود");
            }
            else
            {
                var NewLocation = _mapper.Map(dto, user);
                var result = await _userManager.UpdateAsync(user);
                return ApiResponse<bool>.Ok(true, "تم تحديث عنوانك بنجاح");
            }
        }

        public async Task<ApiResponse<bool>> UpdateNameAsync(string userId, UpdateNameDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("المستخدم غير موجود");
            }
            else
            {
                _mapper.Map(dto, user);
                var result = await _userManager.UpdateAsync(user);
                return ApiResponse<bool>.Ok(true, "تم تحديث الاسم بنجاح");
            }
        }


        public async Task<ApiResponse<bool>> UpdateProfilImageesync(string userId, UpdateProfileImageDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (dto.ProfileImage is not null)
            {
                var NewImg = await _Image.SaveFileAsync(dto.ProfileImage, "ProfileImages");

                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    _Image.DeleteFile(user.ProfileImage);
                }
                user.ProfileImage = NewImg;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    string.Join(", ", result.Errors.Select(e => e.Description));
                    //throw new BadRequestException("حدث خطأ اثناء محاولة اضافة صورة");
                    return ApiResponse<bool>.Ok(false, "حدث خطأ اثناء محاولة اضافة صورة");
                }
            }
            return ApiResponse<bool>.Ok(true, "تمت تغيير صورة الملف الشخصي بنجاح بنجاح  ");

        }

        public async Task<ApiResponse<bool>> RemoveProfileImageAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                throw new NotFoundException("المستخدم غير موجود.");

            if (string.IsNullOrWhiteSpace(user.ProfileImage))
                return ApiResponse<bool>.Ok(true, "لا توجد صورة شخصية لحذفها.");

            _Image.DeleteFile(user.ProfileImage);

            user.ProfileImage = null;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new BadRequestException("حدث خطأ أثناء حذف الصورة الشخصية.");

            return ApiResponse<bool>.Ok(true, "تم حذف الصورة الشخصية بنجاح.");
        }

        #region UPDATE OLD 
        public async Task<ApiResponse<bool>> UpdateProfileInfoAsync(string userId, UpdateProfileInfoDTO dto)
        {
            _logger.LogInformation("Update User Info with Id: {UserId} at {Time}", userId, DateTime.UtcNow);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User With Id :{UserId} Not Found at {Time}", userId, DateTime.UtcNow);
                throw new NotFoundException("المستخدم غير موجود.");
            }

            #region Email
            //var originalEmail = user.Email;
            //if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            //{
            //    // التأكد أن الإيميل الجديد غير مستخدم
            //    var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            //    if (existingUser is not null)
            //        throw new InvalidOperationException("Email already exists.");

            //    user.Email = dto.Email;
            //    user.UserName = dto.Email;
            //    user.EmailConfirmed = false;

            //    // Generate Token
            //    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            //    // Send Confirmation Email
            //}

            //Claude

            //if (!string.Equals(originalEmail, dto.Email, StringComparison.OrdinalIgnoreCase))
            //{
            //    var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            //    if (existingUser is not null)
            //        throw new InvalidOperationException("Email already exists.");

            //    user.Email = dto.Email;
            //    user.UserName = dto.Email;
            //    user.EmailConfirmed = false;

            //    // لازم تعمل UpdateAsync الأول عشان تقدر تبعت التوكن
            //    var updateResult = await _userManager.UpdateAsync(user);
            //    if (!updateResult.Succeeded)
            //        throw new InvalidOperationException("Failed to update email.");

            //    return true;
            //}
            #endregion

            //src dest
            _mapper.Map(dto, user);
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(
                    user,
                    dto.CurrentPassword!,
                    dto.NewPassword);

                if (!changePasswordResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Failed to change password for UserId: {UserId}. Errors: {Errors}",
                        userId,
                        string.Join(", ", changePasswordResult.Errors.Select(e => e.Description)));

                    throw new BadRequestException(
                        string.Join(", ", changePasswordResult.Errors.Select(e => e.Description)));
                }
            }


            #region Id Image

            if (dto.IdentificationImage is not null)
            {
                if (user.VerificationStatus == VerificationStatus.Verified)
                {
                    throw new BadRequestException("صورة البطاقة موجودة بالفعل .");
                }
                var newIdImage =
                 await _Image.SaveFileAsync(dto.IdentificationImage, "Identification");
                if (!string.IsNullOrEmpty(user.IdentificationImage))
                {
                    _Image.DeleteFile(user.IdentificationImage);
                }

                user.IdentificationImage = newIdImage;
                user.VerificationStatus = VerificationStatus.Pending;
            }

            #endregion

            #region Profile Image

            if (dto.ProfileImage is not null)
            {
                var NewImg = await _Image.SaveFileAsync(dto.ProfileImage, "Profile");

                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    _Image.DeleteFile(user.ProfileImage);
                }
                user.ProfileImage = NewImg;
            }
            #endregion

            var result = await _userManager.UpdateAsync(user);

            #region EMAIL
            //if (!string.Equals(originalEmail, dto.Email, StringComparison.OrdinalIgnoreCase))
            //{
            //    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //    await _emailService.SendConfirmationEmailAsync(dto.Email, token);
            //}
            #endregion

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update profile for UserId: {UserId}. Errors: {Errors}",
                userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));

                throw new BadRequestException("خطأ في تعديل بيانات المستخدم.");
            }

            _logger.LogInformation("Profile updated successfully for UserId: {UserId}", userId);
            return ApiResponse<bool>.Ok(true, "تم تعديل البيانات بنجاح");

        }
        #endregion

        #endregion
    }

}

