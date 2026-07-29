using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class CreateCaseResponseDto
    {
        public bool IsCreated { get; set; }

        public bool IsBlocked { get; set; }

        public long? CaseId { get; set; }

        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; init; } = [];
    }
}