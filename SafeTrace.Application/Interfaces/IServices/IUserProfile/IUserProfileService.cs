using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;

namespace SafeTrace.Application.Interfaces.IServices.IUserProfile
{
    public interface IUserProfileService
    {
        Task<ApiResponse<GetUserInfoDTO?>> GetProfileInfoAsync(string userId);
        Task<ApiResponse<VisitUserDTO?>> GetVisitedUserAsync(string userId);
        Task<ApiResponse<bool>> UpdateProfileInfoAsync(string userId, UpdateProfileInfoDTO dto);
        Task<ApiResponse<bool>> RemoveProfileImageAsync(string userId);
        Task<ApiResponse<bool>> UpdateNameAsync(string userId, UpdateNameDTO dto);
        Task<ApiResponse<bool>> UpdateProfilImageesync(string userId, UpdateProfileImageDTO dto);
        Task<ApiResponse<bool>> UpdateHomeLocationAsync(string userId, UpdateHomeLocationDTO dto);
        Task<ApiResponse<bool>> UpdatePhoneNumberAsync(string userId, ChangePhoneNumberDTO dto);
        Task<ApiResponse<bool>> AddIdImageAsync(string userId, AddIdImageDTO dto);

    }


}
