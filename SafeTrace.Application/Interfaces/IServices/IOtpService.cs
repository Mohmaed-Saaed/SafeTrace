using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IOtpService
    {
        Task<string> GenerateAndSaveOtpAsync(string userId, OtpType type);
        Task<bool> ValidateOtpAsync(string userId, string code, OtpType type);
    }
}