using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    /// <summary>
    /// Result of ICaseHelperService.CheckDuplicateAsync. Pure detection output — no
    /// business decision baked in. HasDuplicate just means "at least one verified
    /// candidate exists"; it's up to the caller (each case service) to decide what
    /// that means for its own case type (hard block, potential match, or ignore).
    /// </summary>
    public class MatchedCasesResult
    {
        public bool HasMatched { get; init; }

        public IReadOnlyList<MatchedCaseDto> DuplicateCases { get; init; } = [];

        public static MatchedCasesResult Empty => new()
        {
            HasMatched = false,
            DuplicateCases = []
        };
    }
}

