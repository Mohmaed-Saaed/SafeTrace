using SafeTrace.Application.DTOs.FacebookImportedPosts.Request;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices.IFacebookIntegration
{
    public interface IFacebookImportedPostService
    {
        Task<List<FacebookImportedPostListDto>> GetAllAsync(FacebookPostFilterDto filter);
        Task<FacebookImportedPostDetailDto> GetByIdAsync(long id);
        Task<FacebookImportedPostDetailDto> UpdateAsync(long id, UpdateFacebookImportedPostDto dto);
        Task<FacebookImportedPostDetailDto> RejectAsync(long id);
        Task<PublishFacebookImportedPostResponseDto> PublishAsync(long id, PublishFacebookImportedPostRequestDto? dto = null);
    }
}
