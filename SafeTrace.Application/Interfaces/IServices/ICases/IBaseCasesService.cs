using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IBaseCasesService<TListDto, TDetailDto, TFilterDto>
        where TFilterDto : CasesFilterBaseDto
    {
        Task<ApiResponse<PaginationResponseDto<TListDto>>> GetAllAsync(TFilterDto filter);
        Task<ApiResponse<TDetailDto>> GetByIdAsync(long id);
        Task<ApiResponse<PaginationResponseDto<TListDto>>> GetMyCasesAsync(string userId, TFilterDto filter);
        Task<ApiResponse<PaginationResponseDto<TDetailDto>>> AdminGetAllAsync(TFilterDto filter);
 
        Task ApproveAsync(long caseId);
        Task RejectAsync(long caseId);
        Task SoftDeleteAsync(long caseId, string userId, bool isAdmin = false, bool checkOwnership = true);
        Task MarkAsFoundAsync(long caseId, string userId, FoundPersonInfo? foundPersonInfo = null, bool isAdmin = false, bool checkOwnership = true);
        Task PermanentDeleteAsync(long caseId);
    }
}