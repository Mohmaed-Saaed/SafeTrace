using SafeTrace.Application.DTOs.UrgentCase.Request;
using SafeTrace.Application.DTOs.UrgentCase.Response;


namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IUrgentCaseService : IBaseCasesService<UrgentCaseListDto, UrgentCaseDetailDto, UrgentCasesFilterDto>
    {
        Task<ApiResponse<string>> CreateAsync(string userId, UrgentCaseCreateDto createDto);
        Task<ApiResponse<string>> UpdateAsync(long id, string userId, UrgentCaseUpdateDto updateDto);
    }
}