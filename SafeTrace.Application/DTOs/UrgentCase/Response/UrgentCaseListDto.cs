using SafeTrace.Application.DTOs.Cases.Response;

namespace SafeTrace.Application.DTOs.UrgentCase.Response
{
    public class UrgentCaseListDto : CaseListItemBaseDto
    {
        public DateTime? EndDate { get; set; }
    }
}

