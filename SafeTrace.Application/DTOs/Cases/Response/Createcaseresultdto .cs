using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class CreateCaseResultDto
    {
        public bool IsCreated { get; set; }

        public long? CaseId { get; set; }

        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; set; } = [];
    }
}