using SafeTrace.Application.DTOs.LongTermCase.Request;
using SafeTrace.Application.DTOs.LongTermCase.Response;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface ILongTermCaseService : IBaseCasesService<LongTermCaseListDto, LongTermCaseDetailDto, LongTermCaseFilterDto>
    {
        Task<ApiResponse<string>> CreateAsync(CreateLongTermCaseDto dto, string userId);
        Task<ApiResponse<string>> UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId);
    }
}