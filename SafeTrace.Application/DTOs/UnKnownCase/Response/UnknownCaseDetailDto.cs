using SafeTrace.Application.DTOs.Cases.Response;

namespace SafeTrace.Application.DTOs.UnKnownCase.Response
{
    public class UnknownCaseDetailDto : CaseDetailBaseDto
    {
        public List<RelatedUnknownCaseDto> RelatedCases { get; set; } = [];
    }
}