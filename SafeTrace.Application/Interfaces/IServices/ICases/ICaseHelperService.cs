using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Cases.Response;


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
        /// Generic pre-create duplicate check used by all case types (LongTerm / Unknown / Urgent).
        /// - No match found                            -> DuplicateCheckResult.None (safe to create).
        /// - A match with the SAME case type            -> throws BadRequestException (true duplicate, never bypassable).
        /// - A match with a DIFFERENT case type          -> returns RequiresConfirmation = true with the matches,
        ///                                                   unless forceCreate = true, in which case it's bypassed.
        /// </summary>
        Task<DuplicateCheckResult> CheckDuplicateCaseAsync(
            CaseType currentCaseType,
            CaseMatchSubjectInfoDto subject,
            IFormFile primaryImage,
            bool forceCreate = false);

    }
}