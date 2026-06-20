
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.Interfaces.IServices;
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

        public UserProfileService(UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<UserProfileService> logger,
            IFileStorageService Image)
        {

            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
            _Image = Image;
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

            _mapper.Map(dto, user);

            if (dto.ProfileImage is not null)
            {
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    _Image.DeleteFile(user.ProfileImage);
                }
                user.ProfileImage =
                    await _Image.SaveFileAsync(dto.ProfileImage, "Profile");
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new InvalidOperationException("Failed to update profile.");

            return result.Succeeded;
        }
    }

}
