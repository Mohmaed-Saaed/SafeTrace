using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class DuplicateCheckResult
    {
        public bool IsBlocked { get; init; }

        public DuplicateDecision DuplicateDecision { get; init; } = DuplicateDecision.None;

        public long? ExistingCaseId { get; init; }

        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; init; } = [];
    }
}