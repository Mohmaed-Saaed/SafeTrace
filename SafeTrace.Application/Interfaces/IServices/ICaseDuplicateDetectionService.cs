using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface ICaseDuplicateDetectionService
    {
        Task<CaseDuplicateDetectionResultDto> FindDuplicateAsync(FacebookImportedPost post);
    }
}
