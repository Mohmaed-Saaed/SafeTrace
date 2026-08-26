namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Response
{
    public sealed record CaseDuplicateDetectionResultDto(
        bool IsDuplicate,
        long? ExistingCaseId = null,
        string? ExistingCaseCode = null,
        CaseType? ExistingCaseType = null,
        string? Reason = null)
    {
        public static CaseDuplicateDetectionResultDto None { get; } = new(false);
    }
}
