using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.DTOs.Auth.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Extensions;
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
        private readonly HttpClient _httpClient;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
                throw new ConflictException("Email is already registered.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = _mapper.Map<ApplicationUser>(registerDto);
                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new BadRequestException($"Registration failed: {errors}");
                }

                await _userManager.AddToRoleAsync(user, "User");
                var otp = await GenerateAndSaveOtpInternalAsync(user.Id, OtpType.EmailConfirmation);

                await _unitOfWork.CommitTransactionAsync();

                try
                {
                    var mailBody = BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "تأكيد الحساب الرقمي", "شكراً لتسجيلك في منصة SafeTrace. يرجى استخدام رمز التحقق التالي لتفعيل حسابك وتأكيد البريد الإلكتروني الخاص بك.");
                    await _emailService.SendEmailAsync(user.Email!, "SafeTrace - رمز تفعيل الحساب", mailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send activation email to {Email}", user.Email);
                }

                return ApiResponse<string>.Ok(user.Id, "User registered successfully. Activation email has been dispatched.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                _logger.LogWarning("Failed authentication challenge for user: {Email}", loginDto.Email);
                throw new UnauthorizedException("Invalid email or password.");
            }

            if (!user.EmailConfirmed)
                throw new ForbiddenException("Please verify your identity via email confirmation before attempting access.");

            var otp = await GenerateAndSaveOtpInternalAsync(user.Id, OtpType.LoginVerification);

            try
            {
                var mailBody = BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "رمز التحقق الثنائي لتسجيل الدخول", "لقد تم رصد محاولة تسجيل دخول إلى حسابك. للحفاظ على أمان بياناتك، يرجى استخدام رمز الأمان المؤقت التالي لإتمام العملية.");
                await _emailService.SendEmailAsync(user.Email!, "SafeTrace - رمز أمان تسجيل الدخول", mailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send login 2FA OTP to {Email}", user.Email);
                throw new BadRequestException("An error occurred while dispatching security properties.");
            }

            return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto { RequiresOtp = true, Email = user.Email! }, "Two-factor authorization code issued.");
        }

        public async Task<ApiResponse<AuthResponseDto>> VerifyLoginOtpAsync(VerifyLoginDto verifyLoginDto)
        {
            var user = await _userManager.FindByEmailAsync(verifyLoginDto.Email);
            if (user == null) throw new NotFoundException("Identity profile mismatch.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var isValidOtp = await ValidateOtpInternalAsync(user.Id, verifyLoginDto.OtpCode, OtpType.LoginVerification);
                if (!isValidOtp) throw new BadRequestException("The security code entered is invalid or has expired.");

                var result = await GenerateAuthTokensAndSaveAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<AuthResponseDto>.Ok(result, "Session established successfully.");
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
                    throw new UnauthorizedException("Invalid Google token payload validation.");

                using var doc = JsonDocument.Parse(await googleResponse.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                var email = root.GetProperty("email").GetString();
                var firstName = root.TryGetProperty("given_name", out var fName) ? fName.GetString() : "Google";
                var lastName = root.TryGetProperty("family_name", out var lName) ? lName.GetString() : "User";

                if (string.IsNullOrEmpty(email))
                    throw new BadRequestException("Google authorization scope missing mandatory email references.");

                return await ProcessExternalUserFlowAsync(email, firstName!, lastName!, "Google");
            }
            catch (Exception ex) when (ex is not WebException && ex is not UnauthorizedException && ex is not BadRequestException)
            {
                _logger.LogError(ex, "Critical external network execution exception failure inside Google authentication payload handling.");
                throw new BadRequestException("External social integration payload connection failure.");
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> FacebookLoginAsync(ExternalLoginDto externalLoginDto)
        {
            try
            {
                var verifyUrl = $"https://graph.facebook.com/me?fields=id,email,first_name,last_name&access_token={externalLoginDto.ProviderToken}";
                var fbResponse = await _httpClient.GetAsync(verifyUrl);
                if (!fbResponse.IsSuccessStatusCode)
                    throw new UnauthorizedException("Invalid Facebook authorization access token validation.");

                using var doc = JsonDocument.Parse(await fbResponse.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                if (!root.TryGetProperty("email", out var emailProp) || string.IsNullOrEmpty(emailProp.GetString()))
                    throw new BadRequestException("Facebook privacy scope configuration does not allow access to a valid verified email profile.");

                var email = emailProp.GetString();
                var firstName = root.TryGetProperty("first_name", out var fName) ? fName.GetString() : "Facebook";
                var lastName = root.TryGetProperty("last_name", out var lName) ? lName.GetString() : "User";

                return await ProcessExternalUserFlowAsync(email!, firstName!, lastName!, "Facebook");
            }
            catch (Exception ex) when (ex is not WebException && ex is not UnauthorizedException && ex is not BadRequestException)
            {
                _logger.LogError(ex, "Critical provider identity synchronization validation error during Facebook runtime execution.");
                throw new BadRequestException("Facebook endpoint interaction failed.");
            }
        }

        public async Task<ApiResponse<string>> ConfirmEmailAsync(string email, string otpCode)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new NotFoundException("Profile structure not found.");

            var isValid = await ValidateOtpInternalAsync(user.Id, otpCode, OtpType.EmailConfirmation);
            if (!isValid) throw new BadRequestException("Invalid or expired account validation token references.");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            return ApiResponse<string>.Ok(user.Id, "Email confirmed successfully.");
        }

        public async Task<ApiResponse<string>> ResendOtpAsync(string email, OtpType type)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new NotFoundException("Target user references do not map to active identity rows.");

            var otp = await GenerateAndSaveOtpInternalAsync(user.Id, type);
            var subject = type == OtpType.EmailConfirmation ? "رمز تفعيل الحساب" : "رمز الأمان الخاص بك";
            var mailBody = BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "طلب رمز تحقق جديد", "بناءً على طلبك، تم إصدار رمز أمان بديل جديد. يرجى إدخاله لإكمال العملية الجارية.");

            await _emailService.SendEmailAsync(user.Email!, $"SafeTrace - {subject}", mailBody);

            return ApiResponse<string>.Ok(null, "Security verification credentials re-dispatched.");
        }

        public async Task<ApiResponse<string>> ForgetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new NotFoundException("Target identity target not structured within current persistence contexts.");

            var otp = await GenerateAndSaveOtpInternalAsync(user.Id, OtpType.PasswordReset);
            var mailBody = BuildArabicOtpEmailTemplate($"{user.FName} {user.LName}", otp, "طلب إعادة تعيين كلمة المرور", "لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك. يرجى استخدام الرمز السري التالي لإتمام عملية التعيين بنجاح.");

            await _emailService.SendEmailAsync(user.Email!, "SafeTrace - إعادة تعيين كلمة المرور", mailBody);

            return ApiResponse<string>.Ok(null, "Recovery operations dispatched.");
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null) throw new NotFoundException("Identity entity missing.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var isValid = await ValidateOtpInternalAsync(user.Id, resetPasswordDto.OtpCode, OtpType.PasswordReset);
                if (!isValid) throw new BadRequestException("Invalid credentials verification provided.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, resetPasswordDto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new BadRequestException($"State change rejected: {errors}");
                }

                await _unitOfWork.CommitTransactionAsync();
                return ApiResponse<string>.Ok(null, "Credential modifications saved.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto requestDto)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(requestDto.ExpiredAccessToken);
            if (principal == null) throw new BadRequestException("Invalid active cryptographic security framework mappings.");

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var storedRefreshToken = await _unitOfWork.Repository<RefreshToken>()
                    .GetOneAsync(t => t.Token == requestDto.RefreshToken && t.UserId == userId);

                if (storedRefreshToken == null || !storedRefreshToken.IsActive)
                    throw new UnauthorizedException("Target session claims structures invalidated or expired.");

                var user = await _userManager.FindByIdAsync(userId!);
                if (user == null) throw new NotFoundException("User contextual mapping profiles missing.");

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
                    IsVerified = user.IsVerified
                }, "Session criteria securely updated.");
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
            if (storedToken == null) throw new NotFoundException("Token registration context metadata missing.");
            if (!storedToken.IsActive) throw new BadRequestException("Token execution scope already mapped as dead.");

            storedToken.RevokedAt = DateTime.UtcNow;
            _unitOfWork.Repository<RefreshToken>().Update(storedToken);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(null, "Target access vector successfully terminated.");
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
                IsVerified = user.IsVerified
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
                        IsVerified = false
                    };

                    var identityResult = await _userManager.CreateAsync(user);
                    if (!identityResult.Succeeded)
                    {
                        var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                        throw new BadRequestException($"External integration parsing failure: {errors}");
                    }

                    await _userManager.AddToRoleAsync(user, "User");
                }

                var userLoginInfo = await _userManager.GetLoginsAsync(user);
                if (userLoginInfo.All(l => l.LoginProvider != provider))
                {
                    var loginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, email, provider.ToUpper()));
                    if (!loginResult.Succeeded)
                        throw new BadRequestException("Federated provider bindings identity context syncing execution exception failure.");
                }

                var responseData = await GenerateAuthTokensAndSaveAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<AuthResponseDto>.Ok(responseData, "Social authentication context mapped successfully.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task<string> GenerateAndSaveOtpInternalAsync(string userId, OtpType type)
        {
            var existingOtps = await _unitOfWork.Repository<UserOtp>().Query().Where(o => o.UserId == userId && o.Type == type && !o.IsUsed).ToListAsync();
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
            return randomCode;
        }

        private async Task<bool> ValidateOtpInternalAsync(string userId, string code, OtpType type)
        {
            var userOtp = await _unitOfWork.Repository<UserOtp>().GetOneAsync(o => o.UserId == userId && 
                                                                          o.Code == code && 
                                                                          o.Type == type && 
                                                                          !o.IsUsed && 
                                                                          o.ExpiryTime > DateTime.UtcNow);
            if (userOtp == null) return false;

            userOtp.IsUsed = true;
            _unitOfWork.Repository<UserOtp>().Update(userOtp);
            await _unitOfWork.SaveAsync();
            return true;
        }

        private static string BuildArabicOtpEmailTemplate(string fullName, string otpCode, string contextTitle, string contextualDescription)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة SafeTrace</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6; margin-bottom: 30px;'>{contextualDescription}</p>
                
                <div style='background: #f8fafc; border: 1px dashed #cbd5e1; padding: 20px; border-radius: 10px; text-align: center; margin-bottom: 30px;'>
                    <span style='font-size: 12px; color: #64748b; display: block; margin-bottom: 8px; font-weight: 500;'>{contextTitle}</span>
                    <span style='font-size: 34px; font-weight: 700; color: #2563eb; letter-spacing: 8px; font-family: monospace; display: inline-block;'>{otpCode}</span>
                </div>
                
                <p style='color: #ef4444; font-size: 13px; font-weight: 500; margin-bottom: 25px;'>تنبيه أمني: تنتهي صلاحية هذا الرمز بعد 10 دقائق تلقائياً. يرجى عدم مشاركة هذا الرمز مع أي شخص كائن من كان لحماية سرية حسابك.</p>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <p style='color: #94a3b8; font-size: 12px; line-height: 1.5; margin: 0;'>إذا لم تقم بإنشاء هذا الحساب أو تقديم هذا الطلب، يمكنك تجاهل هذا البريد الإلكتروني بأمان دون اتخاذ أي إجراء إضافي.</p>
                <br/>
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة SafeTrace</span></p>
            </div>";
        }
    }
}