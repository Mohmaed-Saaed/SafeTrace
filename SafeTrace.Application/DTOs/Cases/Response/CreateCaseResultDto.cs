using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    /// <summary>
    /// Returned by all case-creation endpoints (LongTerm / Unknown / Urgent).
    /// - IsCreated = true  -> the case was created, CaseId is populated.
    /// - IsCreated = false -> a matching case of a DIFFERENT case type was found.
    ///   MatchedCases holds the candidate(s); the client should offer the user the choice to
    ///   chat with the case owner, or resend the request with forceCreate = true to ignore the match.
    /// </summary>
    public class CreateCaseResultDto
    {
        public bool IsCreated { get; set; }

        public long? CaseId { get; set; }

        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; init; } = [];
    }
}