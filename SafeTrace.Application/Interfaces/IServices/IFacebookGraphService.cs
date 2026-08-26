using SafeTrace.Application.Models.Facebook;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFacebookGraphService
    {
        Task<FacebookPageConnectionResult> ConnectPageAsync(string facebookPageId);
        Task<FacebookPageConnectionResult> ReconnectPageAsync(string facebookPageId);
        Task<IReadOnlyList<FacebookPostDto>> GetNewPostsAsync(
            FacebookPage page,
            DateTimeOffset? since,
            CancellationToken cancellationToken = default);
    }
}
