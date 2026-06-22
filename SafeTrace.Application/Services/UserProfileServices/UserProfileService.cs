
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Services.UserProfileServices
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserProfileService> _logger;
        private readonly IFileStorageService _Image;
        private readonly INotificationServices _Notify;

        public UserProfileService(UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<UserProfileService> logger,
            IFileStorageService Image,
         INotificationServices Notify)
        {

            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
            _Image = Image;
            Notify = _Notify;
        }

        #region Test
        public async Task<List<GetAllDTO>> GetAllUsersAsync()
        {
            _logger.LogInformation("Get All Users Info Test" + DateTime.Now);
            var users = await _userManager.Users.ToListAsync();

            return _mapper.Map<List<GetAllDTO>>(users);
        }
        #endregion

        public async Task<GetUserInfoDTO?> GetProfileInfoAsync(string userId)
        {
            _logger.LogInformation($"Get User Info with Id: {userId} in {DateTime.Now}");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User With Id{userId} Not Found in {DateTime.Now}");
                throw new KeyNotFoundException($"User With Id{userId} Not Found");
            }
            else
            {
                return _mapper.Map<GetUserInfoDTO>(user);
            }
        }

        public async Task<bool> UpdateProfileInfoAsync(string userId, UpdateProfileInfoDTO dto)
        {
            _logger.LogInformation($"Update User Info with Id: {userId} in {DateTime.Now}");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User With Id{userId} Not Found in {DateTime.Now}");
                throw new KeyNotFoundException("User not found");
            }

            var originalEmail = user.Email;
            //src dest





            #region Email
            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                // التأكد أن الإيميل الجديد غير مستخدم
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);

                if (existingUser is not null)
                    throw new InvalidOperationException("Email already exists.");

                user.Email = dto.Email;
                user.UserName = dto.Email;
                user.EmailConfirmed = false;

                // Generate Token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Send Confirmation Email
            }

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

            _mapper.Map(dto, user);

            #region Id Image
            if (dto.IdentificationImage is not null)
            {
                if (user.IsVerified)
                {
                    throw new InvalidOperationException(
                        "Identification image has already been approved.");
                }

                if (!string.IsNullOrEmpty(user.IdentificationImage))
                {
                    _Image.DeleteFile(user.IdentificationImage);
                }

                user.IdentificationImage =
                    await _Image.SaveFileAsync(dto.IdentificationImage, "Identification");
            }

            #endregion

            #region Profile Image

            if (dto.ProfileImage is not null)
            {
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    _Image.DeleteFile(user.ProfileImage);
                }
                user.ProfileImage =
                    await _Image.SaveFileAsync(dto.ProfileImage, "Profile");
            }
            #endregion


            var result = await _userManager.UpdateAsync(user);

            //if (!string.Equals(originalEmail, dto.Email, StringComparison.OrdinalIgnoreCase))
            //{
            //    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //    await _emailService.SendConfirmationEmailAsync(dto.Email, token);
            //}

            if (!result.Succeeded)
                throw new InvalidOperationException("Failed to update profile.");

            return result.Succeeded;
        }
    }

}
