using SafeTrace.Application.DTOs.AiCaseAnalyzer.Request;
using SafeTrace.Application.DTOs.AiCaseAnalyzer.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IAiCaseAnalyzerService
    {
        Task<SocialPostAiResultDto> AnalyzeAsync(SocialPostAiInputDto input);
    }
}
