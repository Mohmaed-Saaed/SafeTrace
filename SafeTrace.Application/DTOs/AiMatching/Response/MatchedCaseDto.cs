using SafeTrace.Application.DTOs.Cases.Response;

namespace SafeTrace.Application.DTOs.AiMatching.Response
{
    public class MatchedCaseDto : CaseListItemBaseDto
    {
        public float Similarity { get; set; }
    }
}