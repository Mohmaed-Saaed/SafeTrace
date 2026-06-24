using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
using SafeTrace.Application.DTOs.UrgentMissingCase.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IUrgentCaseService
    {
        Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetAllAsync(UrgentCaseFilterDto filter);
        Task<ApiResponse<PaginationResponseDto<UrgentCaseAdminDto>>> AdminGetAllAsync(UrgentCaseFilterDto filter);
        Task<ApiResponse<UrgentCaseDetailDto>> GetByIdAsync(long id);
        Task<ApiResponse<PaginationResponseDto<UrgentCaseListItemDto>>> GetMyCasesAsync(UrgentCaseFilterDto filter);
        Task<ApiResponse<UrgentCaseDetailDto>> CreateAsync(UrgentCaseCreateDto createDto);
        // Task<ApiResponse<UrgentCaseDetailDto>> UpdateAsync(UrgentCaseUpdateDto updateDto);
        // Task<ApiResponse<string>> DeleteAsync(long id);
        // Task<ApiResponse<string>> PermanentDeleteAsync(long id);
        // Task<ApiResponse<string>> ApproveAsync(long id);
        // Task<ApiResponse<string>> RejectAsync(long id);
        // Task<ApiResponse<string>> MarkAsFoundedAsync(long id);
    }
}