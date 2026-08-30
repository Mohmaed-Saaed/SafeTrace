using SafeTrace.Application.DTOs.FacebookImportedPosts.Request;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;
using SafeTrace.Application.DTOs.Responses;

namespace SafeTrace.Application.Interfaces.IServices.IFacebookIntegration
{
    public interface IFacebookImportedPostService
    {
        Task<ApiResponse<List<FacebookImportedPostListDto>>> GetAllAsync(FacebookPostFilterDto filter);
        Task<ApiResponse<FacebookImportedPostDetailDto>> GetByIdAsync(long id);
        Task<ApiResponse<FacebookImportedPostDetailDto>> UpdateAsync(long id, UpdateFacebookImportedPostDto dto);
        Task<ApiResponse<FacebookImportedPostDetailDto>> RejectAsync(long id);
        Task<ApiResponse<PublishFacebookImportedPostResponseDto>> PublishAsync(long id, PublishFacebookImportedPostRequestDto? dto = null);
    }
}
