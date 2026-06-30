using SafeTrace.Application.DTOs.CasesMissing.Response;

namespace SafeTrace.Application.Interfaces.IServices.IMissingCases
{
    public interface ICasesService
    {
        Task<IEnumerable<CasesDto>> GetCases();
    }
}
