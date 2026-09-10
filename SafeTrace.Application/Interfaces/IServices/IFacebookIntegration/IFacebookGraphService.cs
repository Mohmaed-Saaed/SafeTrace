using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.FacebookPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices.IFacebookIntegration
{
    public interface IFacebookGraphService
    {
        Task<FacebookPageProfileDto> GetPageProfileAsync(string pageAccessToken);

        Task<DateTimeOffset?> DebugTokenAsync(string pageAccessToken);

        Task<IReadOnlyList<FacebookPostDto>> GetNewPostsAsync(
            string facebookPageId,
            string pageAccessToken,
            DateTimeOffset? since);
    }
}
