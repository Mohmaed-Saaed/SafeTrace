using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.Responses;

namespace SafeTrace.Application.Interfaces.IServices.IFacebookIntegration
{
    public interface IFacebookPageService
    {
        Task<ApiResponse<FacebookPageResponseDto>> IntegrateAsync(IntegrateFacebookPageDto dto);
        Task<ApiResponse<List<FacebookPageResponseDto>>> GetAllAsync(FacebookPageFilterDto? filter = null);
        Task<ApiResponse<string>> DisconnectAsync(long id);
        Task<ApiResponse<FacebookPageResponseDto>> ReconnectAsync(long id, ReconnectFacebookPageDto dto);
        Task<ApiResponse<string>> DeleteAsync(long id);
    }
}
