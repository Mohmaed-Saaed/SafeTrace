using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class MatchedCasesResult
    {
        public bool HasMatches { get; init; }

        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; init; } = [];

        public static MatchedCasesResult Empty => new()
        {
            HasMatches = false,
            MatchedCases = []
        };
    }
}

