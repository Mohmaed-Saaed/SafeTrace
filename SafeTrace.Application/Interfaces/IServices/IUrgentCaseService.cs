using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
using SafeTrace.Application.DTOs.UrgentMissingCase.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IUrgentCaseService
    {
        Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetAllAsync(string userId, UrgentCaseFilterDto filter);
        Task<ApiResponse<PaginationResponseDto<UrgentCaseAdminDto>>> AdminGetAllAsync(string userId, UrgentCaseFilterDto filter);
        Task<ApiResponse<UrgentCaseDetailDto>> GetByIdAsync(long id);
        Task<ApiResponse<UrgentCaseDetailDto>> CreateAsync(string userId, UrgentCaseCreateDto createDto);
        Task<ApiResponse<UrgentCaseDetailDto>> UpdateAsync(string userId, UrgentCaseUpdateDto updateDto);
        Task<ApiResponse<string>> DeleteAsync(long id);
        Task<ApiResponse<string>> PermanentDeleteAsync(long id);
        Task<ApiResponse<string>> MarkAsFoundedAsync(long id);
    }
}