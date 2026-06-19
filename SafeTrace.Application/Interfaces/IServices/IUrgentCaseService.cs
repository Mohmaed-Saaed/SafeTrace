using SafeTrace.Application.DTOs.UrgentMissingCase;

namespace SafeTrace.Application.Interfaces.IServices{
    public interface IUrgentCaseService
    {
        Task<ApiResponse<IEnumerable<UrgentCaseListItemDto>>> GetAllAsync(UrgentCaseFilterDto filter);
        // Task<ApiResponse<UrgentCaseDetailWithRelatedDto>> GetByIdAsync(long id);
    }
}

