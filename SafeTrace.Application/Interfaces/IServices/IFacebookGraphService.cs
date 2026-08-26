using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.FacebookPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFacebookGraphService
    {
        Task<FacebookPageConnectionResultDto> ConnectPageAsync(string facebookPageId);
        Task<FacebookPageConnectionResultDto> ReconnectPageAsync(string facebookPageId);
        Task<IReadOnlyList<FacebookPostDto>> GetNewPostsAsync(
            FacebookPage page,
            DateTimeOffset? since);
    }
}
