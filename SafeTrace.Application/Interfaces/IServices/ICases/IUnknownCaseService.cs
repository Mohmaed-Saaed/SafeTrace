using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.DTOs.UnKnownCase.Response;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IUnknownCaseService : IBaseCasesService<UnknownCaseListDto, UnknownCaseDetailDto, UnknownCasesFilterDto>
    {
        Task<ApiResponse<string>> CreateUnknownCaseAsync(CreateUnknownDto dto, string userId);
        Task<ApiResponse<string>> UpdateUnknownCaseAsync(long id, UpdateUnknownCaseDto dto, string userId);
    }
}