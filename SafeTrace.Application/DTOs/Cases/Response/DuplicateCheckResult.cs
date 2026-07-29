using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class DuplicateCheckResult
    {
        public bool IsBlocked { get; init; }

        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; init; } = [];
    }
}