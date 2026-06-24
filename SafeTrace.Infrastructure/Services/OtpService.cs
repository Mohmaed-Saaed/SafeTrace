using Microsoft.Extensions.Logging;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OtpService> _logger;

        public OtpService(IUnitOfWork unitOfWork, ILogger<OtpService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string> GenerateAndSaveOtpAsync(string userId, OtpType type)
        {
            var existingOtps = await _unitOfWork.Repository<UserOtp>().Query()
                                                                      .Where(o => o.UserId == userId && o.Type == type && !o.IsUsed)
                                                                      .ToListAsync(); 
            foreach (var existingOtp in existingOtps)
            {
                existingOtp.IsUsed = true;
                _unitOfWork.Repository<UserOtp>().Update(existingOtp);
            }

            var randomCode = new Random().Next(100000, 999999).ToString();
            var userOtp = new UserOtp
            {
                Code = randomCode,
                Type = type,
                ExpiryTime = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                UserId = userId
            };

            await _unitOfWork.Repository<UserOtp>().CreateAsync(userOtp);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("A new OTP of type {OtpType} was generated for User ID: {UserId}", type.ToString(), userId);

            return randomCode;
        }

        public async Task<bool> ValidateOtpAsync(string userId, string code, OtpType type)
        {
            var userOtp = await _unitOfWork.Repository<UserOtp>().GetOneAsync(o => o.UserId == userId && o.Code == code && o.Type == type && !o.IsUsed && o.ExpiryTime > DateTime.UtcNow);

            if (userOtp == null)
            {
                _logger.LogWarning("Invalid or expired OTP validation attempt for User ID: {UserId}, Type: {OtpType}", userId, type.ToString());
                return false;
            }

            userOtp.IsUsed = true;
            _unitOfWork.Repository<UserOtp>().Update(userOtp);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("OTP successfully validated for User ID: {UserId}, Type: {OtpType}", userId, type.ToString());

            return true;
        }
    }
}