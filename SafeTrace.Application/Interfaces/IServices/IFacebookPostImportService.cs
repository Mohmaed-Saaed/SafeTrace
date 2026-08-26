using SafeTrace.Application.DTOs.FacebookPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFacebookPostImportService
    {
        Task<int> ImportAsync(
            FacebookPage page,
            IReadOnlyList<FacebookPostDto> posts,
            CancellationToken cancellationToken = default);
    }
}
