using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
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