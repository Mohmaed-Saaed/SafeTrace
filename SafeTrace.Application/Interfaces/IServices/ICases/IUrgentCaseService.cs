using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.UrgentCase.Request;
using SafeTrace.Application.DTOs.UrgentCase.Response;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IUrgentCaseService : IBaseCasesService<UrgentCaseListDto, UrgentCaseDetailDto, UrgentCasesFilterDto>
    {
        Task<ApiResponse<CreateCaseResponseDto>> CreateAsync(string userId, UrgentCaseCreateDto createDto, bool forceCreate = false);
        Task<ApiResponse<string>> UpdateAsync(long id, string userId, UrgentCaseUpdateDto updateDto);
        Task<ApiResponse<UrgentCreationStatusResponse>> GetUrgentCreationStatusAsync(string userId);
    }
}