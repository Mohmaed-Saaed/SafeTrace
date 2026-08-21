using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.LongTermCase.Request;
using SafeTrace.Application.DTOs.LongTermCase.Response;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface ILongTermCaseService : IBaseCasesService<LongTermCaseListDto, LongTermCaseDetailDto, LongTermCaseFilterDto>
    {
        Task<ApiResponse<CreateCaseResponseDto>> CreateAsync(string userId, CreateLongTermCaseDto dto, bool forceCreate = false);
        Task<ApiResponse<string>> UpdateAsync(
            long id,
            string userId,
            UpdateLongTermCaseDto dto);
    }
}
