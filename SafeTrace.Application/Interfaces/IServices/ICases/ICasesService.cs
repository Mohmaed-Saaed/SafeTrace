using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.SharedCases;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    /// <summary>
    /// Operations shared across every Case type. Controllers for individual
    /// case types (Urgent, LongTerm, Unknown, ...) call this for anything
    /// that is identical regardless of case type; they call their own
    /// feature service only for Create/Update and type-specific logic.
    ///
    /// Standardized contract: one signature per operation, one return type,
    /// no in-service try/catch — exceptions propagate to the global
    /// exception-handling middleware.
    /// </summary>
    public interface ICasesService
    {
        // ── Queries ──────────────────────────────────────────────────
        Task<ApiResponse<PaginationResponseDto<CaseListItemDto>>> GetCasesAsync(CasesFilterDto filter, Point? userLocation = null, string? userId = null);
        Task<ApiResponse<CaseDetailDto>> GetCaseByIdAsync(long id);
        Task<ApiResponse<PaginationResponseDto<CaseListItemDto>>> GetMyCasesAsync(string userId, CasesFilterDto filter);
        Task<ApiResponse<PaginationResponseDto<CaseDetailDto>>> AdminGetCasesAsync(CasesFilterDto filter);

        // ── Shared commands — single signature each, exceptions propagate ──
        Task ApproveAsync(long caseId);

        Task RejectAsync(long caseId);

        Task SoftDeleteAsync(
            long caseId,
            string userId,
            bool isAdmin = false,
            bool checkOwnership = true);

        Task MarkAsFoundAsync(
            long caseId,
            string userId,
            FoundPersonInfo? foundPersonInfo = null,
            bool isAdmin = false,
            bool checkOwnership = true);

        Task PermanentDeleteAsync(long caseId);
    }
}