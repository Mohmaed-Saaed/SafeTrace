using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.DTOs.UnKnownCase.Response;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IUnknownCaseService : IBaseCasesService<UnknownCaseListDto, UnknownCaseDetailDto, UnknownCasesFilterDto>
    {
        Task<ApiResponse<CreateCaseResponseDto>> CreateUnknownCaseAsync(string userId, CreateUnknownDto dto, bool forceCreate = false);
        Task<ApiResponse<string>> UpdateUnknownCaseAsync(
            long id,
            string userId,
            UpdateUnknownCaseDto dto);
    }
}
