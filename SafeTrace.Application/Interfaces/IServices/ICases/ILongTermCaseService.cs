using SafeTrace.Application.DTOs.LongTermCases.Request;

namespace SafeTrace.Application.Interfaces.IServices.ICases
{
    public interface ILongTermCaseService
    {
        Task<long> CreateAsync(CreateLongTermCaseDto dto, string userId);
        Task UpdateAsync(long id, UpdateLongTermCaseDto dto, string userId, bool isAdmin);
    }
}