using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Cases.Response;
using System.Linq.Expressions;


namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface ICaseHelperService
    {
        Task<TEntity> GetValidCaseAsync<TEntity>(
            long id,
            string? userId = null,
            bool checkOwnership = false,
            bool allowDeleted = false,
            bool tracked = true,
            params Expression<Func<TEntity, object>>[] includes)
            where TEntity : Case;

        void ValidateCaseIsEditable(Case entity);

        Task ValidateVerifiedUserAsync(string userId);

        Task<string> GenerateCaseCodeAsync(CaseCodePrefix prefix);

        Task<int> ResolveAgeCategoryIdAsync(int age);

        Task<List<CaseFile>> CreateCaseFilesAsync(
            IFormFile primaryImage,
            IEnumerable<IFormFile>? additionalImages,
            IFormFile? video,
            string folderName,
            long caseId = 0);   

        void SetPrimaryImage(ICollection<CaseFile> files, long primaryPhotoId);

        void CleanupPhysicalFiles(IEnumerable<string> filePaths);

        Task DeleteFacesAsync(IEnumerable<string> faceIds, long caseIdForLogging);

        Task<MatchedCasesResult> FindMatchedCasesAsync(CaseMatchSubjectInfoDto subject, IFormFile primaryImage);

        /// <summary>
        /// Generic pre-create duplicate DETECTION used by all case types (LongTerm / Unknown / Urgent).
        /// This only detects and categorizes matches — it does NOT decide what to do with them.
        /// Each case service inspects the result (SameTypeMatch / CrossTypeMatches) and applies
        /// its own behavior (e.g. block, merge, or ask for confirmation).
        /// </summary>
        Task<DuplicateCheckResult> CheckDuplicateCaseAsync(
            CaseType currentCaseType,
            CaseMatchSubjectInfoDto subject,
            IFormFile primaryImage,
            Func<MatchedCaseDto, Task> onSameTypeMatchAsync,
            bool forceCreate = false);

    }
}