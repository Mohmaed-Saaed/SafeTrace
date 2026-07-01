using SafeTrace.Application.DTOs.UrgentCase.Request;
using SafeTrace.Application.DTOs.UrgentCase.Response;


namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IUrgentCaseService
    {
        Task<ApiResponse<UrgentCaseCreateResponse>> CreateAsync(string userId, UrgentCaseCreateDto createDto);
        Task<ApiResponse<UrgentCaseUpdateResponse>> UpdateAsync(string userId, UrgentCaseUpdateDto updateDto);
    }
}