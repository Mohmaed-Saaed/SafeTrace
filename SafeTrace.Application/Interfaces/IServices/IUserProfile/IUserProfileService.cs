using SafeTrace.Application.DTOs.User_Profiel_DTOS;

namespace SafeTrace.Application.Interfaces.IServices.IUserProfile
{
    public interface IUserProfileService
    {
        Task<GetUserInfoDTO?> GetProfileInfoAsync(string userId);
        Task<List<GetAllDTO>> GetAllUsersAsync();
        Task<bool> UpdateProfileInfoAsync(string userId, UpdateProfileInfoDTO dto);
    }
}
