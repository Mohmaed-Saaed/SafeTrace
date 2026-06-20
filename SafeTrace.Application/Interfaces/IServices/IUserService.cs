using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IUserService
    {
        Task<ApiResponse<PaginationResponseDto<GetUserDto>>> GetAllUsersAsync(UserFilterDto filterDto);
        Task<ApiResponse<string>> ChangeUserRoleAsync(string userId, ChangeUserRoleDto dto);
        Task<ApiResponse<UserPermissionsResponseDto>> GetUserPermissionsAsync(string userId);
        Task<ApiResponse<string>> AssignUserPermissionsAsync(string userId, AssignUserPermissionsDto dto);
    }
}