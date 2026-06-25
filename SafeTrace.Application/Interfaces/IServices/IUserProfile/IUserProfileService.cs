using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;

namespace SafeTrace.Application.Interfaces.IServices.IUserProfile
{
    public interface IUserProfileService
    {
        Task<ApiResponse<GetUserInfoDTO?>> GetProfileInfoAsync(string userId);
        Task<ApiResponse<List<GetAllDTO>>> GetAllUsersAsync();
        Task<ApiResponse<bool>> UpdateProfileInfoAsync(string userId, UpdateProfileInfoDTO dto);
    }
}
