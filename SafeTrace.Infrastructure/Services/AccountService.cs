using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.DTOs.Auth.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Domain.Enums;
using System.Net;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using UAParser;

namespace SafeTrace.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountService> _logger;
        private readonly IOtpService _otpService;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationServices _notificationService;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IOtpService otpService,
            ILogger<AccountService> logger,
            IHttpContextAccessor httpContextAccessor,
            INotificationServices notificationService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _otpService = otpService;
            _httpClient = new HttpClient();
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
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
                    var mailBody = EmailTemplates.BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "تأكيد الحساب الرقمي", "شكراً لتسجيلك في منصة لقاء. يرجى استخدام رمز التحقق التالي لتفعيل حسابك وتأكيد البريد الإلكتروني الخاص بك.");
                    await _emailService.SendEmailAsync(user.Email!, "لقاء - رمز تفعيل الحساب", mailBody);
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
            if (user == null)
            {
                throw new UnauthorizedException("البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            }

            CheckIfUserIsBlocked(user);

            if (!await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                await _userManager.AccessFailedAsync(user);
                
                if (await _userManager.IsLockedOutAsync(user))
                {
                    throw new ForbiddenException("تم حظر الحساب مؤقتاً لتجاوز الحد المسموح لمحاولات الدخول الخاطئة. يرجى المحاولة لاحقاً.");
                }

                throw new UnauthorizedException("البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            }

            if (!user.EmailConfirmed)
            {
                throw new ForbiddenException("يرجى تأكيد بريدك الإلكتروني أولاً قبل تسجيل الدخول.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var authResult = await GenerateAuthTokensAndSaveAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                await SendLoginAlertAsync(user);

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

            var mailBody = EmailTemplates.BuildEmailConfirmedSuccessTemplate(user.FName);
            _ = _emailService.SendEmailAsync(user.Email!, "لقاء - تم تأكيد بريدك الإلكتروني", mailBody);

            _ = _notificationService.SendNotificationAsync(new SendNotificationDTO
            {
                UserId = user.Id,
                Content = "تهانينا! تم تأكيد عنوان بريدك الإلكتروني بنجاح.",
                Type = NotificationType.System
            });

            return ApiResponse<string>.Ok(null, "تم تأكيد البريد الإلكتروني بنجاح.");
        }

        public async Task<ApiResponse<string>> ResendOtpAsync(string email, OtpType type)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

            var otp = await _otpService.GenerateAndSaveOtpAsync(user.Id, type);
            var subject = type == OtpType.EmailConfirmation ? "رمز تفعيل الحساب" : "رمز الأمان الخاص بك";
            var mailBody = EmailTemplates.BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "طلب رمز تحقق جديد", "بناءً على طلبك، تم إصدار رمز أمان بديل جديد. يرجى إدخاله لإكمال العملية الجارية.");

            await _emailService.SendEmailAsync(user.Email!, $"لقاء - {subject}", mailBody);

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

            await _emailService.SendEmailAsync(user.Email!, "لقاء - إعادة تعيين كلمة المرور", mailBody);

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

                var mailBody = EmailTemplates.BuildPasswordResetSuccessTemplate(user.FName);
                _ = _emailService.SendEmailAsync(user.Email!, "لقاء - تأكيد إعادة تعيين كلمة المرور", mailBody);

                _ = _notificationService.SendNotificationAsync(new SendNotificationDTO
                {
                    UserId = user.Id,
                    Content = "تم إعادة تعيين كلمة المرور بنجاح وتسجيل الخروج من كافة الأجهزة.",
                    Type = NotificationType.System
                });

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
                    if (result.Errors.Any(e => e.Code == "PasswordMismatch")) throw new BadRequestException("كلمة المرور الحالية غير صحيحة.");
                    throw new BadRequestException("حدث خطأ أثناء تغيير كلمة المرور، يرجى التأكد من الشروط المطلوبة.");
                }

                var currentRefreshToken = GetRefreshTokenFromCookie();
                await RevokeAllActiveSessionsAsync(userId, currentRefreshToken);

                await _unitOfWork.CommitTransactionAsync();

                var mailBody = EmailTemplates.BuildPasswordResetSuccessTemplate(user.FName);
                _ = _emailService.SendEmailAsync(user.Email!, "لقاء - تأكيد تغيير كلمة المرور", mailBody);

                _ = _notificationService.SendNotificationAsync(new SendNotificationDTO
                {
                    UserId = user.Id,
                    Content = "تم تغيير كلمة المرور بنجاح وتسجيل الخروج من كافة الأجهزة الأخرى.",
                    Type = NotificationType.System
                });

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
            catch (Exception ex) when (ex is not WebException && ex is not UnauthorizedException && ex is not BadRequestException && ex is not ForbiddenException)
            {
                _logger.LogError(ex, "Critical external network execution exception failure inside Google authentication payload handling.");
                throw new BadRequestException("حدث خطأ أثناء محاولة تسجيل الدخول بواسطة جوجل.");
            }
        }



        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync()
        {
            var refreshTokenFromCookie = GetRefreshTokenFromCookie();
            if (string.IsNullOrEmpty(refreshTokenFromCookie))
                throw new UnauthorizedException("انتهت صلاحية الجلسة، يرجى تسجيل الدخول من جديد.");

            var semaphore = _refreshLocks.GetOrAdd(refreshTokenFromCookie, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                var storedRefreshToken = await _unitOfWork.Repository<RefreshToken>()
                    .GetOneAsync(t => t.Token == refreshTokenFromCookie);

                if (storedRefreshToken == null) throw new UnauthorizedException("انتهت صلاحية الجلسة، يرجى تسجيل الدخول من جديد.");

                var userId = storedRefreshToken.UserId;

                if (!storedRefreshToken.IsActive)
                {
                    bool isWithinGracePeriod = storedRefreshToken.RevokedAt != null && 
                        (DateTime.UtcNow - storedRefreshToken.RevokedAt.Value).TotalSeconds <= 60;

                    if (!isWithinGracePeriod)
                    {
                        if (storedRefreshToken.ReplacedByToken != null)
                        {
                            await RevokeAllActiveSessionsAsync(userId!);
                            await _unitOfWork.SaveAsync();
                            await _unitOfWork.CommitTransactionAsync();
                            _logger.LogWarning("Token reuse detected for user {UserId}. Revoking all active sessions.", userId);
                            throw new UnauthorizedException("تم اكتشاف نشاط مريب في الجلسة، تم تسجيل الخروج من جميع الأجهزة كإجراء أمني.");
                        }

                        throw new UnauthorizedException("انتهت صلاحية الجلسة، يرجى تسجيل الدخول من جديد.");
                    }
                }

                var user = await _userManager.FindByIdAsync(userId!);
                if (user == null) throw new NotFoundException("هذا الحساب غير موجود.");

                var newRefreshToken = _tokenService.GenerateRefreshToken();
                storedRefreshToken.RevokedAt = DateTime.UtcNow;
                storedRefreshToken.ReplacedByToken = newRefreshToken.Token;

                newRefreshToken.UserId = user.Id;
                _unitOfWork.Repository<RefreshToken>().Update(storedRefreshToken);
                await _unitOfWork.Repository<RefreshToken>().CreateAsync(newRefreshToken);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";
                var newAccessToken = _tokenService.GenerateAccessToken(user, role);

                SetRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.ExpiresAt);

                return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshTokenExpiration = newRefreshToken.ExpiresAt,
                    Email = user.Email!,
                    FullName = $"{user.FName} {user.LName}",
                    ProfileImage = user.ProfileImage,
                    VerificationStatus = user.VerificationStatus
                }, "تم تجديد الجلسة بنجاح.");
            }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            finally
            {
                semaphore.Release();
                if (semaphore.CurrentCount == 1)
                {
                    _refreshLocks.TryRemove(refreshTokenFromCookie, out _);
                }
            }
        }

        public async Task<ApiResponse<string>> RevokeTokenAsync()
        {
            var cookieToken = GetRefreshTokenFromCookie();
            if (string.IsNullOrEmpty(cookieToken)) return ApiResponse<string>.Ok(null, "تم تسجيل الخروج بنجاح.");

            var storedToken = await _unitOfWork.Repository<RefreshToken>().GetOneAsync(t => t.Token == cookieToken);
            if (storedToken != null && storedToken.IsActive)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                _unitOfWork.Repository<RefreshToken>().Update(storedToken);
                await _unitOfWork.SaveAsync();
            }

            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });

            return ApiResponse<string>.Ok(null, "تم تسجيل الخروج بنجاح.");
        }

        private async Task<AuthResponseDto> GenerateAuthTokensAndSaveAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";
            var accessToken = _tokenService.GenerateAccessToken(user, role);
            var refreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.UserId = user.Id;
            await _unitOfWork.Repository<RefreshToken>().CreateAsync(refreshToken);
            await _unitOfWork.SaveAsync();

            SetRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiresAt);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshTokenExpiration = refreshToken.ExpiresAt,
                Email = user.Email!,
                FullName = $"{user.FName} {user.LName}",
                ProfileImage = user.ProfileImage,
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

                await SendLoginAlertAsync(user);

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
                if (user.LockoutEnd.Value == DateTimeOffset.MaxValue)
                {
                    _logger.LogWarning("Action denied. Blocked user {Email} attempted an account mutation operation.", user.Email);
                    throw new ForbiddenException("هذا الحساب محظور من قبل الإدارة.");
                }
                else
                {
                    _logger.LogWarning("Action denied. Temporarily locked out user {Email} attempted an account operation.", user.Email);
                    throw new ForbiddenException("تم حظر الحساب مؤقتاً لتجاوز الحد المسموح لمحاولات تسجيل الدخول. يرجى المحاولة لاحقاً.");
                }
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
                await _unitOfWork.SaveAsync();
            }
        }

        private async Task SendLoginAlertAsync(ApplicationUser user)
        {
            try
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "غير معروف";
                var userAgentStr = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
                
                string browser = "غير معروف";
                string os = "غير معروف";

                if (!string.IsNullOrEmpty(userAgentStr))
                {
                    var uaParser = Parser.GetDefault();
                    var clientInfo = uaParser.Parse(userAgentStr);
                    browser = clientInfo.UA.Family;
                    os = clientInfo.OS.Family;
                }

                var mailBody = EmailTemplates.BuildLoginAlertTemplate(user.FName, ipAddress, browser, os);
                _ = _emailService.SendEmailAsync(user.Email!, "لقاء - تنبيه أمني: تسجيل دخول جديد", mailBody);

                _ = _notificationService.SendNotificationAsync(new SendNotificationDTO
                {
                    UserId = user.Id,
                    Content = $"تم تسجيل دخول جديد لحسابك من جهاز: {os} ({browser}). إذا لم تكن أنت، يرجى تغيير كلمة المرور فوراً.",
                    Type = NotificationType.System
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send login alert for User {Email}", user.Email);
            }
        }

        private void SetRefreshTokenCookie(string token, DateTime expiresAt)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expiresAt
            };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string? GetRefreshTokenFromCookie()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];
        }
    }
}