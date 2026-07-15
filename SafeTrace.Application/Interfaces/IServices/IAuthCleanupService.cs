namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IAuthCleanupService
    {
        Task CleanupExpiredOtpsAsync();
        Task CleanupOldRefreshTokensAsync();
    }
}
