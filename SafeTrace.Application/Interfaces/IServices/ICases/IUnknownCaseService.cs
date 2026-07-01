using SafeTrace.Application.DTOs.UnKnownCase.Request;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IUnknownCaseService
    {
        Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto, string userId);
        Task<ApiResponse<string>> UpdateUnknownCaseAsync(long id, UpdateUnknownCaseDto dto, string userId);
    }
}