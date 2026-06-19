
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Services.UserProfileServices
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserProfileService> _logger;
        public UserProfileService(UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<UserProfileService> logger)
        {

            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
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
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new InvalidOperationException("Failed to update profile.");

            return result.Succeeded;
        }
    }

}
