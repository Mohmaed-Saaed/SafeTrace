using SafeTrace.Application.DTOs.FacebookPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices.IFacebookIntegration
{
    public interface IAiCaseAnalyzerService
    {
        Task<SocialPostAiResultDto> AnalyzeAsync(string text);
    }
}
