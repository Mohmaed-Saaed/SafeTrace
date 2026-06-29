using SafeTrace.Application.Common.Models;
using SafeTrace.Application.DTOs.LongTermCases;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface ILongTermCaseService
    {
        Task<PagedResultDto<LongTermCaseCardDto>> GetAllAsync(LongTermCaseFilterDto filter);

        Task<IEnumerable<LongTermCaseCardDto>> GetFoundedCasesAsync();

        // Admin-only: soft-deleted cases kept for review

        // includeDeleted = true lets Admins open a soft-deleted case's details
        Task<LongTermCaseDetailsDto> GetByIdAsync(long id, bool includeDeleted = false);

        Task<IEnumerable<LongTermCaseCardDto>> GetMyCasesAsync(string userId);


        Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId);

        // Throws NotFoundException / ForbiddenException instead of returning bool
        Task UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin);

        // Soft delete (sets IsDeleted = true)
        Task DeleteAsync(long id, string userId, bool isAdmin);


        // Admin-only: hard delete (only allowed on already soft-deleted cases)
        Task PermanentDeleteAsync(long id);

        Task ApproveAsync(long id);

        Task RejectAsync(long id);

        Task MarkAsFoundedAsync(long id, MarkAsFoundedDto dto, string userId, bool isAdmin);
        Task<IEnumerable<LongTermCaseCardDto>> GetAdminCasesAsync(CaseStatus? status);
    }
}
