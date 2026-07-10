using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.DTOs.UnKnownCase.Response;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IUnknownCaseService : IBaseCasesService<UnknownCaseListDto, UnknownCaseDetailDto, UnknownCasesFilterDto>
    {
        Task<ApiResponse<string>> CreateUnknownCaseAsync(string userId, CreateUnknownDto dto);
        Task<ApiResponse<string>> UpdateUnknownCaseAsync(long id, string userId, UpdateUnknownCaseDto dto);
    }
}