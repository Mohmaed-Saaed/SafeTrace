using SafeTrace.Application.DTOs.MissingCases.Request;
using SafeTrace.Application.DTOs.UrgentMissingCase;

namespace SafeTrace.Application.Interfaces.IServices{
    public interface IUrgentCaseService
    {
        Task<ApiResponse<IEnumerable<UrgentCaseListItemDto>>> GetAllAsync(FilterCasesDto filter);
        // Task<ApiResponse<UrgentCaseDetailWithRelatedDto>> GetByIdAsync(long id);
    }
}

