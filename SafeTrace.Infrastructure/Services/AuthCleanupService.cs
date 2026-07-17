using Microsoft.Extensions.Logging;

namespace SafeTrace.Infrastructure.Services
{
    public class AuthCleanupService : IAuthCleanupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthCleanupService> _logger;

        public AuthCleanupService(IUnitOfWork unitOfWork, ILogger<AuthCleanupService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task CleanupExpiredOtpsAsync()
        {
            var now = DateTime.UtcNow;

            var deletedCount = await _unitOfWork.Repository<UserOtp>().Query()
                .Where(o => o.ExpiryTime < now)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                _logger.LogInformation("Successfully deleted {Count} expired OTPs.", deletedCount);
            }
        }

        public async Task CleanupOldRefreshTokensAsync()
        {
            var expirationThreshold = DateTime.UtcNow.AddDays(-7);

            var deletedCount = await _unitOfWork.Repository<RefreshToken>().Query()
                .Where(rt => rt.ExpiresAt < expirationThreshold || (rt.RevokedAt != null && rt.RevokedAt < expirationThreshold))
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                _logger.LogInformation("Successfully deleted {Count} old Refresh Tokens.", deletedCount);
            }
        }
    }
}
