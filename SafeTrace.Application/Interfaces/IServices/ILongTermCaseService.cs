using SafeTrace.Application.Common.Models;
using SafeTrace.Application.DTOs.LongTermCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.Interfaces.IServices
{
    /// <summary>
    /// Business logic for the Long-Term Missing Cases module (SRS section 2.4 / FR-18 .. FR-25, FR-47 .. FR-50).
    /// </summary>
    public interface ILongTermCaseService
    {
        /// <summary>Public list with search/filter/sort/pagination (FR-21 .. FR-25). Only Active cases are returned.</summary>
        Task<PagedResultDto<LongTermCaseCardDto>> GetAllAsync(LongTermCaseFilterDto filter);

        /// <summary>Full details for the case details page.</summary>
        Task<LongTermCaseDetailsDto?> GetByIdAsync(long id);

        /// <summary>Cases reported by the current user ("My Cases").</summary>
        Task<IEnumerable<LongTermCaseCardDto>> GetMyCasesAsync(string userId);

        /// <summary>Cases with Status == Found, shown in the Founded Cases section (FR-50 .. FR-53).</summary>
        Task<IEnumerable<LongTermCaseCardDto>> GetFoundedCasesAsync();

        /// <summary>Admin queue - cases awaiting approval (FR-20).</summary>
        Task<IEnumerable<LongTermCaseCardDto>> GetPendingCasesAsync();

        /// <summary>Creates a new case with Status = Pending (FR-18 .. FR-20). Returns the new case Id.</summary>
        Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId);

        /// <summary>Updates a case. Only the owner or an Admin may update it.</summary>
        Task<bool> UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin);

        /// <summary>Deletes a case. Only the owner or an Admin may delete it.</summary>
        Task<bool> DeleteAsync(long id, string userId, bool isAdmin);

        /// <summary>Admin approval - Pending -> Active (FR-20, FR-21).</summary>
        Task<bool> ApproveAsync(long id);

        /// <summary>Admin rejection - Pending -> Closed.</summary>
        Task<bool> RejectAsync(long id);

        /// <summary>Mark as Founded - Active -> Found, creates FoundPersonInfo (FR-47 .. FR-50).</summary>
        Task<bool> MarkAsFoundedAsync(long id, MarkAsFoundedDto dto, string userId, bool isAdmin);
    }
}
