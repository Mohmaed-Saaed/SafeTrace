using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Responses;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IAIMatchingService
    {
        Task<ApiResponse<List<MatchedCaseDto>>> GetMatchingCasesAsync(IFormFile image, string userId);
    }
}