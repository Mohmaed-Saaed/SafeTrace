using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface IBaseCasesService<TListDto, TDetailDto, TFilterDto>
        where TFilterDto : CasesFilterBaseDto
    {
        Task<ApiResponse<PaginationResponseDto<TListDto>>> GetAllAsync(TFilterDto filter);
        Task<ApiResponse<PaginationResponseDto<TDetailDto>>> AdminGetAllAsync(TFilterDto filter);
        Task<ApiResponse<TDetailDto>> GetByIdAsync(long id);
        Task<ApiResponse<TDetailDto>> AdminGetByIdAsync(long id);
 
        Task<ApiResponse<string>> ApproveAsync(long caseId);
        Task<ApiResponse<string>> RejectAsync(long caseId);
        Task<ApiResponse<string>> SoftDeleteAsync(long caseId, string userId, bool checkOwnership = true);
        Task<ApiResponse<string>> MarkAsFoundAsync(long caseId, string userId, FoundPersonInfoRequestDto foundPersonInfo, bool checkOwnership = true);
        Task<ApiResponse<string>> PermanentDeleteAsync(long caseId);
    }
}