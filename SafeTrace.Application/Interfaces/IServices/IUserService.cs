using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IUserService
    {
        Task<ApiResponse<PaginationResponseDto<GetUserDto>>> GetAllUsersAsync(UserFilterDto filterDto);
        Task<ApiResponse<GetUserDto>> GetUserByIdAsync(string userId);
        Task<ApiResponse<string>> ApproveUserAsync(string userId);
        Task<ApiResponse<string>> RejectUserAsync(string userId);
        Task<ApiResponse<string>> ToggleUserBlockStatusAsync(string userId);
        Task<ApiResponse<string>> ChangeUserRoleAsync(ChangeUserRoleDto dto);
        Task<ApiResponse<UserPermissionsResponseDto>> GetUserPermissionsAsync(string userId);
        Task<ApiResponse<string>> AssignUserPermissionsAsync(AssignUserPermissionsDto dto);
    }
}