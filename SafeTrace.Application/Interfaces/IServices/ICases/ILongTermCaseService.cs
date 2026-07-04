using SafeTrace.Application.DTOs.LongTermCases.Request;
using SafeTrace.Application.DTOs.LongTermCases.Response;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface ILongTermCaseService : IBaseCasesService<LongTermCaseListDto, LongTermCaseDetailDto, LongTermCaseFilterDto>
    {
        Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId);
        Task UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin);
    }
}