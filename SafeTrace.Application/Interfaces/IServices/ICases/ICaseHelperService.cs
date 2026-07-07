using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Cases.Response;


namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    /// <summary>
    /// Reusable Case-related helper logic shared across CasesService and the
    /// per-feature Case services (Urgent / LongTerm / Unknown / ...).
    /// Injected via composition — no service inherits from this.
    /// </summary>
    public interface ICaseHelperService
    {
        // ── Validation / lookup 
        Task<TEntity> GetValidCaseAsync<TEntity>(
            long id,
            string? userId = null,
            bool checkOwnership = false,
            bool allowDeleted = false,
            bool tracked = true,
            params Expression<Func<TEntity, object>>[] includes)
            where TEntity : Case;

        void ValidateCaseIsEditable(Case entity);

        Task<string> GenerateCaseCodeAsync(CaseCodePrefix prefix);
        Task<int> ResolveAgeCategoryIdAsync(int age);

        Task<List<CaseFile>> CreateCaseFilesAsync(
            IFormFile primaryImage,
            IEnumerable<IFormFile>? additionalImages,
            IFormFile? video,
            string folderName,
            long caseId = 0);

        void SetPrimaryImage(ICollection<CaseFile> files, long primaryPhotoId);
        
        // ── File / storage helpers
        void CleanupPhysicalFiles(IEnumerable<string> filePaths);

        // ── Face recognition helpers 
        Task DeleteFacesAsync(IEnumerable<string> faceIds, long caseIdForLogging);

        Task<MatchedCasesResult> FindMatchedCasesAsync(CaseMatchSubjectInfoDto subject, IFormFile primaryImage);
    }
}