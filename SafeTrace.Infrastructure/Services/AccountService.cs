using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.DTOs.Auth.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace SafeTrace.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountService> _logger;
        private readonly IOtpService _otpService;
        private readonly HttpClient _httpClient;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IOtpService otpService,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _otpService = otpService;
            _httpClient = new HttpClient();
        }

        public async Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                _logger.LogWarning("Registration attempt failed: Email {Email} is already in use.", registerDto.Email);
                throw new ConflictException("هذا البريد الإلكتروني مسجل لدينا بالفعل.");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = _mapper.Map<ApplicationUser>(registerDto);
                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user {Email}. Errors: {Errors}", registerDto.Email, errors);
                    throw new BadRequestException("فشلت عملية التسجيل، يرجى المحاولة مرة أخرى.");
                }

                await _userManager.AddToRoleAsync(user, "User");

                var otp = await _otpService.GenerateAndSaveOtpAsync(user.Id, OtpType.EmailConfirmation);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User with ID {UserId} and Email {Email} just created successfully.", user.Id, user.Email);

                try
                {
                    var mailBody = EmailTemplates.BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "تأكيد الحساب الرقمي", "شكراً لتسجيلك في منصة SafeTrace. يرجى استخدام رمز التحقق التالي لتفعيل حسابك وتأكيد البريد الإلكتروني الخاص بك.");
                    await _emailService.SendEmailAsync(user.Email!, "SafeTrace - رمز تفعيل الحساب", mailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send activation email to {Email}", user.Email);
                }

                return ApiResponse<string>.Ok(null, "تم إنشاء الحساب بنجاح. تم إرسال بريد إلكتروني لتفعيل حسابك.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                _logger.LogWarning("Failed authentication challenge for user: {Email}", loginDto.Email);
                throw new UnauthorizedException("البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("User {Email} attempted to login but email is not confirmed.", loginDto.Email);
                throw new ForbiddenException("يرجى تأكيد بريدك الإلكتروني أولاً قبل تسجيل الدخول.");
            }

            CheckIfUserIsBlocked(user);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var authResult = await GenerateAuthTokensAndSaveAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User {Email} logged in successfully.", user.Email);

                return ApiResponse<AuthResponseDto>.Ok(authResult, "تم تسجيل الدخول بنجاح.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<string>> ConfirmEmailAsync(string email, string otpCode)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

            var isValid = await _otpService.ValidateOtpAsync(user.Id, otpCode, OtpType.EmailConfirmation);
            if (!isValid) throw new BadRequestException("رمز التحقق غير صحيح أو انتهت صلاحيته.");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} has successfully confirmed their email address.", email);

            return ApiResponse<string>.Ok(null, "تم تأكيد البريد الإلكتروني بنجاح.");
        }

        public async Task<ApiResponse<string>> ResendOtpAsync(string email, OtpType type)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

            var otp = await _otpService.GenerateAndSaveOtpAsync(user.Id, type);
            var subject = type == OtpType.EmailConfirmation ? "رمز تفعيل الحساب" : "رمز الأمان الخاص بك";
            var mailBody = EmailTemplates.BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "طلب رمز تحقق جديد", "بناءً على طلبك، تم إصدار رمز أمان بديل جديد. يرجى إدخاله لإكمال العملية الجارية.");

            await _emailService.SendEmailAsync(user.Email!, $"SafeTrace - {subject}", mailBody);

            _logger.LogInformation("A new OTP of type {Type} was resent to {Email}.", type.ToString(), email);

            return ApiResponse<string>.Ok(null, "تمت إعادة إرسال رمز التحقق. يرجى مراجعة بريدك الإلكتروني وصندوق الرسائل غير المرغوب فيها (Spam).");
        }

        public async Task<ApiResponse<string>> ForgetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

            CheckIfUserIsBlocked(user);

            var otp = await _otpService.GenerateAndSaveOtpAsync(user.Id, OtpType.PasswordReset);
            var mailBody = EmailTemplates.BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "طلب إعادة تعيين كلمة المرور", "لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك. يرجى استخدام الرمز السري التالي لإتمام عملية التعيين بنجاح.");

            await _emailService.SendEmailAsync(user.Email!, "SafeTrace - إعادة تعيين كلمة المرور", mailBody);

            _logger.LogInformation("Password reset OTP dispatched to {Email}.", email);

            return ApiResponse<string>.Ok(null, "تم إرسال رمز إعادة تعيين كلمة المرور إلى بريدك الإلكتروني.");
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

            CheckIfUserIsBlocked(user);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var isValid = await _otpService.ValidateOtpAsync(user.Id, resetPasswordDto.OtpCode, OtpType.PasswordReset);
                if (!isValid) throw new BadRequestException("رمز التحقق غير صحيح أو انتهت صلاحيته.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, resetPasswordDto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed password reset attempt for user {Email}. Errors: {Errors}", user.Email, errors);
                    throw new BadRequestException("حدث خطأ غير متوقع أثناء إعادة تعيين كلمة المرور.");
                }

                await RevokeAllActiveSessionsAsync(user.Id);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User {Email} has successfully reset their password and all sessions were revoked.", user.Email);

                return ApiResponse<string>.Ok(null, "تم إعادة تعيين كلمة المرور بنجاح.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed password change attempt for user {Email}. Errors: {Errors}", user.Email, errors);

                    if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
                    {
                        throw new BadRequestException("كلمة المرور الحالية غير صحيحة.");
                    }

                    throw new BadRequestException("حدث خطأ أثناء تغيير كلمة المرور، يرجى التأكد من الشروط المطلوبة.");
                }

                await RevokeAllActiveSessionsAsync(userId, dto.CurrentRefreshToken);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User {Email} successfully changed their password and other sessions were revoked.", user.Email);

                return ApiResponse<string>.Ok(null, "تم تغيير كلمة المرور بنجاح وتسجيل الخروج من جميع الأجهزة الأخرى.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(ExternalLoginDto externalLoginDto)
        {
            try
            {
                var verifyUrl = $"https://oauth2.googleapis.com/tokeninfo?id_token={externalLoginDto.ProviderToken}";
                var googleResponse = await _httpClient.GetAsync(verifyUrl);
                if (!googleResponse.IsSuccessStatusCode)
                    throw new UnauthorizedException("فشل التحقق من حساب جوجل الخاص بك.");

                using var doc = JsonDocument.Parse(await googleResponse.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                var email = root.GetProperty("email").GetString();
                var firstName = root.TryGetProperty("given_name", out var fName) ? fName.GetString() : "Google";
                var lastName = root.TryGetProperty("family_name", out var lName) ? lName.GetString() : "User";

                if (string.IsNullOrEmpty(email))
                    throw new BadRequestException("لم نتمكن من الحصول على البريد الإلكتروني من حساب جوجل الخاص بك.");

                return await ProcessExternalUserFlowAsync(email, firstName!, lastName!, "Google");
            }
            catch (Exception ex) when (ex is not WebException && ex is not UnauthorizedException && ex is not BadRequestException)
            {
                _logger.LogError(ex, "Critical external network execution exception failure inside Google authentication payload handling.");
                throw new BadRequestException("حدث خطأ أثناء محاولة تسجيل الدخول بواسطة جوجل.");
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> FacebookLoginAsync(ExternalLoginDto externalLoginDto)
        {
            try
            {
                var verifyUrl = $"https://graph.facebook.com/me?fields=id,email,first_name,last_name&access_token={externalLoginDto.ProviderToken}";
                var fbResponse = await _httpClient.GetAsync(verifyUrl);
                if (!fbResponse.IsSuccessStatusCode)
                    throw new UnauthorizedException("فشل التحقق من حساب فيسبوك الخاص بك.");

                using var doc = JsonDocument.Parse(await fbResponse.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                if (!root.TryGetProperty("email", out var emailProp) || string.IsNullOrEmpty(emailProp.GetString()))
                    throw new BadRequestException("لم نتمكن من الحصول على البريد الإلكتروني من حساب فيسبوك. يرجى إعطاء الصلاحية للوصول للبريد الإلكتروني.");

                var email = emailProp.GetString();
                var firstName = root.TryGetProperty("first_name", out var fName) ? fName.GetString() : "Facebook";
                var lastName = root.TryGetProperty("last_name", out var lName) ? lName.GetString() : "User";

                return await ProcessExternalUserFlowAsync(email!, firstName!, lastName!, "Facebook");
            }
            catch (Exception ex) when (ex is not WebException && ex is not UnauthorizedException && ex is not BadRequestException)
            {
                _logger.LogError(ex, "Critical provider identity synchronization validation error during Facebook runtime execution.");
                throw new BadRequestException("حدث خطأ أثناء محاولة تسجيل الدخول بواسطة فيسبوك.");
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto requestDto)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(requestDto.ExpiredAccessToken);
            if (principal == null) throw new BadRequestException("بيانات الجلسة غير صالحة، يرجى تسجيل الدخول مرة أخرى.");

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var storedRefreshToken = await _unitOfWork.Repository<RefreshToken>()
                    .GetOneAsync(t => t.Token == requestDto.RefreshToken && t.UserId == userId);

                if (storedRefreshToken == null) throw new UnauthorizedException("انتهت صلاحية الجلسة، يرجى تسجيل الدخول من جديد.");

                if (!storedRefreshToken.IsActive)
                {
                    _logger.LogWarning("Refresh Token Reuse Detected! Attempted use of revoked token for user {UserId}. Revoking all sessions...", userId);

                    await RevokeAllActiveSessionsAsync(userId!);
                    await _unitOfWork.CommitTransactionAsync();

                    throw new UnauthorizedException("تم اكتشاف نشاط مريب في الجلسة، تم تسجيل الخروج من جميع الأجهزة كإجراء أمني.");
                }

                var user = await _userManager.FindByIdAsync(userId!);
                if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

                var newRefreshToken = _tokenService.GenerateRefreshToken();
                storedRefreshToken.RevokedAt = DateTime.UtcNow;
                storedRefreshToken.ReplacedByToken = newRefreshToken.Token;

                newRefreshToken.UserId = user.Id;
                _unitOfWork.Repository<RefreshToken>().Update(storedRefreshToken);
                await _unitOfWork.Repository<RefreshToken>().CreateAsync(newRefreshToken);

                await _unitOfWork.CommitTransactionAsync();

                var roles = await _userManager.GetRolesAsync(user);
                var newAccessToken = _tokenService.GenerateAccessToken(user, roles);

                return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken.Token,
                    RefreshTokenExpiration = newRefreshToken.ExpiresAt,
                    Email = user.Email!,
                    FullName = $"{user.FName} {user.LName}",
                    VerificationStatus = user.VerificationStatus
                }, "تم تجديد الجلسة بنجاح.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<string>> RevokeTokenAsync(string token)
        {
            var storedToken = await _unitOfWork.Repository<RefreshToken>().GetOneAsync(t => t.Token == token);
            if (storedToken == null) throw new NotFoundException("بيانات الجلسة غير موجودة.");
            if (!storedToken.IsActive) throw new BadRequestException("هذه الجلسة منتهية بالفعل.");

            storedToken.RevokedAt = DateTime.UtcNow;
            _unitOfWork.Repository<RefreshToken>().Update(storedToken);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(null, "تم تسجيل الخروج بنجاح.");
        }

        private async Task<AuthResponseDto> GenerateAuthTokensAndSaveAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.UserId = user.Id;
            await _unitOfWork.Repository<RefreshToken>().CreateAsync(refreshToken);
            await _unitOfWork.SaveAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiresAt,
                Email = user.Email!,
                FullName = $"{user.FName} {user.LName}",
                VerificationStatus = user.VerificationStatus
            };
        }

        private async Task<ApiResponse<AuthResponseDto>> ProcessExternalUserFlowAsync(string email, string firstName, string lastName, string provider)
        {
            var user = await _userManager.FindByEmailAsync(email);
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = email,
                        UserName = email,
                        FName = firstName,
                        LName = lastName,
                        EmailConfirmed = true,
                        VerificationStatus = VerificationStatus.Unverified
                    };

                    var identityResult = await _userManager.CreateAsync(user);
                    if (!identityResult.Succeeded)
                    {
                        var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                        throw new BadRequestException("فشل في إنشاء الحساب.");
                    }

                    await _userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    CheckIfUserIsBlocked(user);
                }

                var userLoginInfo = await _userManager.GetLoginsAsync(user);
                if (userLoginInfo.All(l => l.LoginProvider != provider))
                {
                    var loginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, email, provider.ToUpper()));
                    if (!loginResult.Succeeded)
                        throw new BadRequestException("حدث خطأ يرجى المحاولة لاحقا.");
                }

                var responseData = await GenerateAuthTokensAndSaveAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<AuthResponseDto>.Ok(responseData, "تم تسجيل الدخول بنجاح.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private void CheckIfUserIsBlocked(ApplicationUser user)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Action denied. Blocked user {Email} attempted an account mutation operation.", user.Email);
                throw new ForbiddenException("هذا الحساب محظور من قبل الإدارة.");
            }
        }

        private async Task RevokeAllActiveSessionsAsync(string userId, string? currentRefreshToken = null)
        {
            var query = _unitOfWork.Repository<RefreshToken>().Query()
                .Where(rt => rt.UserId == userId &&
                             rt.RevokedAt == null &&
                             rt.ExpiresAt > DateTime.UtcNow);

            if (!string.IsNullOrEmpty(currentRefreshToken))
            {
                query = query.Where(rt => rt.Token != currentRefreshToken);
            }

            var activeTokens = await query.ToListAsync();

            if (activeTokens.Any())
            {
                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<RefreshToken>().Update(token);
                }
            }
        }
    }
}