using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    // NOTE: assuming MatchedCaseDto lives in this same namespace (SafeTrace.Application.DTOs.Cases.Response),
    // same as MatchedCasesResult used by CaseHelperService.FindMatchedCasesAsync.
    // If it's actually somewhere else, just add the matching "using" here.

    /// <summary>
    /// Result of a pre-create duplicate check (see ICaseHelperService.CheckDuplicateCaseAsync).
    /// - If no matches were found (or forceCreate was used to bypass a cross-type match),
    ///   RequiresConfirmation is false and creation can proceed.
    /// - A match with the SAME case type never reaches this DTO; it throws a BadRequestException instead.
    /// - A match with a DIFFERENT case type sets RequiresConfirmation = true and populates MatchedCases,
    ///   so the caller can prompt the user to chat with the case owner, or resubmit with forceCreate = true.
    /// </summary>
    public class DuplicateCheckResult
    {
        public bool RequiresConfirmation { get; set; }

        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; init; } = [];

        public static DuplicateCheckResult None => new();
    }
}   