using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class DuplicateCheckResult
    {
        public MatchedCaseDto? SameTypeMatch { get; init; }

        public IReadOnlyList<MatchedCaseDto> CrossTypeMatches { get; init; } = [];

        public bool HasSameTypeMatch => SameTypeMatch is not null;

        public bool HasCrossTypeMatches => CrossTypeMatches.Count > 0;
    }
}   