using Microsoft.EntityFrameworkCore;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Application.Interfaces;
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

            var expiredOtps = await _unitOfWork.Repository<UserOtp>().Query()
                .Where(o => o.ExpiryTime < now)
                .ToListAsync();

            if (expiredOtps.Count > 0)
            {
                _unitOfWork.Repository<UserOtp>().RemoveRange(expiredOtps);
                
                await _unitOfWork.SaveAsync();
                _logger.LogInformation("Successfully deleted {Count} expired OTPs.", expiredOtps.Count);
            }
        }

        public async Task CleanupOldRefreshTokensAsync()
        {
            var expirationThreshold = DateTime.UtcNow.AddDays(-7);

            var oldTokens = await _unitOfWork.Repository<RefreshToken>().Query()
                .Where(rt => rt.ExpiresAt < expirationThreshold || (rt.RevokedAt != null && rt.RevokedAt < expirationThreshold))
                .ToListAsync();

            if (oldTokens.Count > 0)
            {
                _unitOfWork.Repository<RefreshToken>().RemoveRange(oldTokens);
                
                await _unitOfWork.SaveAsync();
                _logger.LogInformation("Successfully deleted {Count} old Refresh Tokens.", oldTokens.Count);
            }
        }
    }
}
