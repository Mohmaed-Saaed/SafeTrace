using SafeTrace.Application.DTOs.FacebookImportedPosts.Request;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFacebookImportedPostAdminService
    {
        Task<List<FacebookImportedPostListDto>> GetAllAsync(FacebookPostFilterDto filter);
        Task<FacebookImportedPostDetailDto> GetByIdAsync(long id);
        Task<FacebookImportedPostDetailDto> UpdateAsync(long id, UpdateFacebookImportedPostDto dto);
        Task<FacebookImportedPostDetailDto> RejectAsync(long id, RejectFacebookImportedPostDto dto);
        Task<PublishFacebookImportedPostResponseDto> PublishAsync(long id);
    }
}
