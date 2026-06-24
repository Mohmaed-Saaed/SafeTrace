using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Application.Services.NotificationServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using static System.Net.Mime.MediaTypeNames;

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
            _logger.LogInformation("Fetching profile for UserId: {userId} at {Time}", userId, DateTime.UtcNow);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User With Id : {UserId} Not Found at {Time}", userId, DateTime.UtcNow);
                throw new NotFoundException($"User Not Found");
            }
            else
            {
                var dto = _mapper.Map<GetUserInfoDTO>(user);
                // to return full path of image 
                var request = _httpContextAccessor.HttpContext.Request;
                string baseUrl = $"{request.Scheme}://{request.Host}";

                dto.ProfileImage = string.IsNullOrEmpty(dto.ProfileImage)
                    ? null
                    : $"{baseUrl}{dto.ProfileImage}";

                dto.IdentificationImage = string.IsNullOrEmpty(dto.IdentificationImage)
                    ? null
                    : $"{baseUrl}{dto.IdentificationImage}";
                return dto;
            }
        }

        public async Task<bool> UpdateProfileInfoAsync(string userId, UpdateProfileInfoDTO dto)
        {
            _logger.LogInformation("Update User Info with Id: {UserId} at {Time}", userId, DateTime.UtcNow);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User With Id :{UserId} Not Found at {Time}", userId, DateTime.UtcNow);
                throw new NotFoundException("User not found");
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

            #region Id Image

            if (dto.IdentificationImage is not null)
            {
                if (user.VerificationStatus == VerificationStatus.Verified)
                {
                    throw new BadRequestException("Identification image has already been approved.");
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

                throw new BadRequestException("Failed to update profile.");
            }

            _logger.LogInformation("Profile updated successfully for UserId: {UserId}", userId);
            return result.Succeeded;

        }

    }

}

