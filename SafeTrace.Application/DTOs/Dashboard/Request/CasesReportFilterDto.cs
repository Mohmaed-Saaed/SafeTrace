using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.Dashboard.Request
{
    public class CasesReportFilterDto : CasesFilterBaseDto
    {
        public CaseType? Type { get; set; }
    }
}
