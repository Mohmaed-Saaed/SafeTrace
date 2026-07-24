using AutoMapper;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Helpers;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly IEmailService _emailService;
        private readonly INotificationServices _notificationService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorageService,
            IEmailService emailService,
            INotificationServices notificationService,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ApiResponse<PaginationResponseDto<GetUserDto>>> GetAllUsersAsync(UserFilterDto filterDto)
        {
            var query = _userManager.Users.AsNoTracking().Where(u => u.EmailConfirmed);

            if (!string.IsNullOrWhiteSpace(filterDto.SearchTerm))
            {
                var term = filterDto.SearchTerm.Trim().ToLower();
                query = query.Where(u => (u.FName + " " + u.LName).Contains(term) ||
                                         u.Email!.Contains(term) ||
                                         u.PhoneNumber!.Contains(term));
            }

            if (filterDto.VerificationStatus.HasValue)
            {
                query = query.Where(u => u.VerificationStatus == filterDto.VerificationStatus.Value);
            }

            if (filterDto.IsBlocked.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                if (filterDto.IsBlocked.Value)
                {
                    query = query.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > now);
                }
                else
                {
                    query = query.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= now);
                }
            }

            if (!string.IsNullOrWhiteSpace(filterDto.RoleId))
            {
                var userIdsInRole = _unitOfWork.Repository<IdentityUserRole<string>>().Query()
                    .Where(ur => ur.RoleId == filterDto.RoleId)
                    .Select(ur => ur.UserId);

                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            var totalCount = await query.CountAsync();

            var userDtosQuery = query.OrderBy(u => u.FName).ThenBy(u => u.LName).Select(u => new GetUserDto
            {
                Id = u.Id,
                FName = u.FName,
                LName = u.LName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                VerificationStatus = u.VerificationStatus,
                IsBlocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
            });

            var userDtos = await userDtosQuery
                                   .Skip((filterDto.PageNumber - 1) * filterDto.PageSize)
                                   .Take(filterDto.PageSize)
                                   .ToListAsync();

            if (userDtos.Any())
            {
                var userIds = userDtos.Select(u => u.Id).ToList();

                var roleQuery = _unitOfWork.Repository<IdentityRole>().Query();
                var userRoleQuery = _unitOfWork.Repository<IdentityUserRole<string>>().Query();

                var userRoles = await (from ur in userRoleQuery
                                       join r in roleQuery on ur.RoleId equals r.Id
                                       where userIds.Contains(ur.UserId)
                                       select new { ur.UserId, RoleName = r.Name })
                                       .ToListAsync();

                var rolesGroupedByUserId = userRoles
                                            .GroupBy(ur => ur.UserId)
                                            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).FirstOrDefault() ?? UserRole.User.ToString());

                foreach (var dto in userDtos)
                {
                    dto.Role = rolesGroupedByUserId.TryGetValue(dto.Id, out var role) ? role : UserRole.User.ToString();
                }
            }

            var paginatedResult = new PaginationResponseDto<GetUserDto>
            {
                Items = userDtos,
                PageNumber = filterDto.PageNumber,
                PageSize = filterDto.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<GetUserDto>>.Ok(paginatedResult, "تم جلب بيانات المستخدمين بنجاح.");
        }

        public async Task<ApiResponse<GetUserByIdDto>> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) throw new NotFoundException("لم يتم العثور على هذا الحساب في النظام.");

            var userDto = _mapper.Map<GetUserByIdDto>(user);

            var roles = await _userManager.GetRolesAsync(user);
            userDto.Role = roles.FirstOrDefault() ?? UserRole.User.ToString();

            return ApiResponse<GetUserByIdDto>.Ok(userDto);
        }

        public async Task<ApiResponse<string>> ChangeUserRoleAsync(string currentUserId, ChangeUserRoleDto dto)
        {
            if (currentUserId == dto.UserId) throw new BadRequestException("لا يمكنك تعديل الصلاحيات أو الدور لحسابك الشخصي.");

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) throw new NotFoundException("لم يتم العثور على هذا الحساب في النظام.");

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
            var currentUserRole = currentUserRoles.FirstOrDefault() ?? UserRole.User.ToString();
            
            var targetUserRoles = await _userManager.GetRolesAsync(user);
            var targetUserRole = targetUserRoles.FirstOrDefault() ?? UserRole.User.ToString();

            if (targetUserRole == UserRole.SuperAdmin.ToString()) 
                throw new ForbiddenException("غير مسموح بالمساس بصلاحيات أو دور المالك الأساسي للنظام.");

            if (currentUserRole == UserRole.Admin.ToString())
            {
                if (targetUserRole == UserRole.Admin.ToString())
                    throw new ForbiddenException("غير مسموح للمسؤول بتعديل صلاحيات أو دور مسؤول آخر.");

                if (dto.NewRole == UserRole.Admin.ToString() || dto.NewRole == UserRole.SuperAdmin.ToString())
                    throw new ForbiddenException("غير مسموح لك بترقية مستخدم إلى مسؤول أو مدير النظام.");
            }
            
            if (dto.NewRole == UserRole.SuperAdmin.ToString())
                throw new ForbiddenException("لا يمكن لأي شخص تعيين صلاحية مدير النظام من لوحة التحكم.");

            var roleExists = await _roleManager.RoleExistsAsync(dto.NewRole);
            if (!roleExists) throw new BadRequestException("الدور (Role) المحدد غير موجود.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault() ?? UserRole.User.ToString();

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new BadRequestException("فشل في إزالة الدور الحالي للمستخدم.");

            var addResult = await _userManager.AddToRoleAsync(user, dto.NewRole);
            if (!addResult.Succeeded) throw new BadRequestException("فشل في تعيين الدور الجديد للمستخدم.");

            if (currentRole != UserRole.User.ToString())
            {
                user.VerificationStatus = VerificationStatus.Unverified;
            }

            if (dto.NewRole != UserRole.User.ToString())
            {
                user.VerificationStatus = VerificationStatus.Verified;
            }
            else
            {
                if(user.IdentificationImage != null)
                {
                    _fileStorageService.DeleteFile(user.IdentificationImage);
                    user.IdentificationImage = null;
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to change user role for UserId: {UserId}. Errors: {Errors}",
                user.Id,
                string.Join(", ", result.Errors.Select(e => e.Description)));

                throw new BadRequestException("تعذر تغيير دور المستخدم. يرجى المحاولة مرة أخرى.");
            }

            var emailBody = EmailTemplates.BuildRoleChangedTemplate(user.FName, dto.NewRole);
            await _emailService.SendEmailAsync(user.Email!, "لقاء - تحديث دورك في منصة لقاء", emailBody);

            await _notificationService.SendNotificationAsync(new SendNotificationDTO
            {
                UserId = user.Id,
                Content = $"تم تغيير دورك في النظام إلى: {TranslateRoleToArabicHelper.TranslateRoleToArabic(dto.NewRole)}",
                Type = NotificationType.System
            });

            _logger.LogWarning($"Role changed for User with ID: {dto.UserId} from {currentRole} to {dto.NewRole}");
            return ApiResponse<string>.Ok(null, "تم تحديث دور المستخدم بنجاح.");
        }

        public async Task<ApiResponse<string>> RegisterByAdminAsync(string currentUserId, RegisterByAdminDto dto)
        {
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
            var currentUserRole = currentUserRoles.FirstOrDefault() ?? UserRole.User.ToString();

            if (currentUserRole == UserRole.Admin.ToString() && (dto.Role == UserRole.Admin.ToString() || dto.Role == UserRole.SuperAdmin.ToString()))
                throw new ForbiddenException("غير مسموح للأدمن بإنشاء حساب بصلاحيات مسؤول أو مدير النظام.");

            if (dto.Role == UserRole.SuperAdmin.ToString())
                throw new ForbiddenException("لا يمكن لأي شخص إنشاء حساب بصلاحية مدير النظام من لوحة التحكم.");

            var userExists = await _userManager.FindByEmailAsync(dto.Email);
            if (userExists != null) throw new ConflictException("هذا البريد الإلكتروني مسجل لدينا بالفعل.");

            var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
            if (!roleExists) throw new NotFoundException($"الدور المسمى '{dto.Role}' غير موجود في النظام.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FName = dto.FName,
                    LName = dto.LName,
                    PhoneNumber = dto.PhoneNumber,
                    EmailConfirmed = true,
                };

                if (dto.Role != UserRole.User.ToString())
                {
                    user.VerificationStatus = VerificationStatus.Verified;
                }
                else
                {
                    user.VerificationStatus = VerificationStatus.Unverified;
                }

                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to register user {Email} by Admin. Errors: {Errors}", dto.Email, errors);
                    throw new BadRequestException("فشلت عملية إنشاء الحساب.");
                }

                await _userManager.AddToRoleAsync(user, dto.Role);
                await _unitOfWork.CommitTransactionAsync();

                var emailBody = EmailTemplates.BuildAdminRegisteredTemplate(user.FName, user.Email, dto.Role);
                await _emailService.SendEmailAsync(user.Email, "لقاء - تم إنشاء حساب لك في منصة لقاء", emailBody);

                await _notificationService.SendNotificationAsync(new SendNotificationDTO
                {
                    UserId = user.Id,
                    Content = "مرحباً بك في منصة لقاء! تم تفعيل حسابك من قِبل الإدارة.",
                    Type = NotificationType.System
                });

                _logger.LogInformation("Admin successfully created user {Email} and assigned role {Role}.", user.Email, dto.Role);

                return ApiResponse<string>.Ok(null, "تم إنشاء الحساب وتعيين الصلاحيات بنجاح.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<string>> ApproveUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("لم يتم العثور على هذا الحساب في النظام.");

            if (user.IdentificationImage == null) throw new BadRequestException("لا توجد صورة هوية (بطاقة) لهذا المستخدم للموافقة عليها.");

            if (user.VerificationStatus != VerificationStatus.Pending) throw new BadRequestException("لا يمكن قبول طلب التوثيق لأنه ليس في حالة انتظار المراجعة.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault() ?? UserRole.User.ToString();

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new BadRequestException("فشل في إزالة الدور الحالي للمستخدم.");

            var addResult = await _userManager.AddToRoleAsync(user, UserRole.VerifiedUser.ToString());
            if (!addResult.Succeeded) throw new BadRequestException("فشل في ترقية حساب المستخدم إلى 'مستخدم موثق'.");

            user.VerificationStatus = VerificationStatus.Verified;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to approve user verification for UserId: {UserId}. Errors: {Errors}",
                userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));

                throw new BadRequestException("تعذر الموافقة على طلب توثيق المستخدم. يرجى المحاولة مرة أخرى.");
            }

            var emailBody = EmailTemplates.BuildVerificationApprovedTemplate(user.FName);
            await _emailService.SendEmailAsync(user.Email!, "لقاء - تم قبول طلب توثيق حسابك", emailBody);

            await _notificationService.SendNotificationAsync(new SendNotificationDTO
            {
                UserId = user.Id,
                Content = "تمت مراجعة هويتك بنجاح. حسابك الآن يمتلك صلاحيات مستخدم موثق.",
                Type = NotificationType.System
            });

            _logger.LogWarning($"Role changed for User with ID: {userId} from {currentRole} to VerifiedUser");
            return ApiResponse<string>.Ok(null, "تمت الموافقة على توثيق المستخدم بنجاح.");
        }

        public async Task<ApiResponse<string>> RejectUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("لم يتم العثور على هذا الحساب في النظام.");

            if (user.IdentificationImage == null) throw new BadRequestException("لا توجد صورة هوية (بطاقة) لهذا المستخدم لرفضها.");

            if (user.VerificationStatus != VerificationStatus.Pending) throw new BadRequestException("لا يمكن رفض طلب التوثيق لأنه ليس في حالة انتظار المراجعة.");


            var DeletedResult = _fileStorageService.DeleteFile(user.IdentificationImage);
            if (!DeletedResult) throw new BadRequestException("فشل في مسح صورة الهوية الخاصة بالمستخدم من الخادم.");

            user.IdentificationImage = null;
            user.VerificationStatus = VerificationStatus.Unverified;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to reject user verification for UserId: {UserId}. Errors: {Errors}",
                userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));

                throw new BadRequestException("تعذر رفض طلب توثيق المستخدم. يرجى المحاولة مرة أخرى.");
            }

            var emailBody = EmailTemplates.BuildVerificationRejectedTemplate(user.FName);
            await _emailService.SendEmailAsync(user.Email!, "لقاء - تم رفض طلب توثيق حسابك", emailBody);

            await _notificationService.SendNotificationAsync(new SendNotificationDTO
            {
                UserId = user.Id,
                Content = "تم رفض طلب توثيق هويتك. يرجى إعادة رفع صورة هوية أكثر وضوحاً ومطابقة للمواصفات.",
                Type = NotificationType.System
            });

            return ApiResponse<string>.Ok(null, "تم رفض طلب توثيق المستخدم بنجاح.");
        }

        public async Task<ApiResponse<string>> ToggleUserBlockStatusAsync(string currentUserId, string userId)
        {
            if (currentUserId == userId) throw new BadRequestException("لا يمكنك حظر حسابك الشخصي.");

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null) throw new NotFoundException("لم يتم العثور على هذا الحساب.");

            if (targetUser.Email == SystemConstants.RootAdminEmail) throw new ForbiddenException("غير مسموح بحظر المدير الأساسي للنظام.");

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
            var targetUserRoles = await _userManager.GetRolesAsync(targetUser);

            var currentUserRole = currentUserRoles.FirstOrDefault() ?? UserRole.User.ToString();
            var targetUserRole = targetUserRoles.FirstOrDefault() ?? UserRole.User.ToString();

            if (targetUserRole == UserRole.SuperAdmin.ToString())
                throw new ForbiddenException("غير مسموح بحظر مدير النظام.");

            if (currentUserRole == UserRole.Admin.ToString() && targetUserRole == UserRole.Admin.ToString())
                throw new ForbiddenException("غير مسموح للمسؤول بحظر مسؤول آخر.");

            bool isCurrentlyBlocked = targetUser.LockoutEnd.HasValue && targetUser.LockoutEnd.Value > DateTimeOffset.UtcNow;

            if (isCurrentlyBlocked)
            {
                await _userManager.SetLockoutEndDateAsync(targetUser, null);

                var emailBody = EmailTemplates.BuildBlockStatusChangedTemplate(targetUser.FName, false);
                await _emailService.SendEmailAsync(targetUser.Email!, "لقاء - تم إلغاء الحظر عن حسابك في منصة لقاء", emailBody);

                await _notificationService.SendNotificationAsync(new SendNotificationDTO
                {
                    UserId = targetUser.Id,
                    Content = "تم إلغاء الحظر عن حسابك. يمكنك استخدام المنصة الآن.",
                    Type = NotificationType.System
                });

                _logger.LogInformation("User {Email} has been unblocked by Admin.", targetUser.Email);
                return ApiResponse<string>.Ok(null, "تم فك الحظر عن المستخدم بنجاح.");
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(targetUser, DateTimeOffset.MaxValue);

                var activeTokens = await _unitOfWork.Repository<RefreshToken>().Query()
                                                                               .Where(rt => rt.UserId == userId &&
                                                                                            rt.RevokedAt == null &&
                                                                                            rt.ExpiresAt > DateTime.UtcNow)
                                                                               .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<RefreshToken>().Update(token);
                }
                await _unitOfWork.SaveAsync();

                var emailBody = EmailTemplates.BuildBlockStatusChangedTemplate(targetUser.FName, true);
                await _emailService.SendEmailAsync(targetUser.Email!, "لقاء - تنبيه: تم حظر حسابك في منصة لقاء", emailBody);

                await _notificationService.SendNotificationAsync(new SendNotificationDTO
                {
                    UserId = targetUser.Id,
                    Content = "تم حظر حسابك بواسطة الإدارة.",
                    Type = NotificationType.System
                });

                _logger.LogInformation("User {Email} has been blocked and all active sessions revoked.", targetUser.Email);
                return ApiResponse<string>.Ok(null, "تم حظر المستخدم وإنهاء جميع جلساته النشطة بنجاح.");
            }
        }

        public async Task<ApiResponse<UserPermissionsResponseDto>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("لم يتم العثور على هذا الحساب.");

            var existingClaims = await _userManager.GetClaimsAsync(user);
            var assignedPermissions = existingClaims.Where(c => c.Type == "Permission")
                                                    .Select(c => c.Value)
                                                    .ToList();

            var allPermissions = Application.Constants.Permissions.GetAllPermissions();

            var response = new UserPermissionsResponseDto
            {
                UserId = user.Id,
                Email = user.Email!,
                Permissions = allPermissions.Select(p => new UserPermissionDto
                {
                    PermissionValue = p,
                    IsSelected = assignedPermissions.Contains(p)
                }).ToList()
            };

            return ApiResponse<UserPermissionsResponseDto>.Ok(response);
        }

        public async Task<ApiResponse<string>> AssignUserPermissionsAsync(string currentUserId, AssignUserPermissionsDto dto)
        {
            if (currentUserId == dto.UserId) throw new BadRequestException("لا يمكنك تعديل الصلاحيات لحسابك الشخصي.");

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) throw new NotFoundException("لم يتم العثور على هذا الحساب.");

            var isSuperAdmin = await _userManager.IsInRoleAsync(user, UserRole.SuperAdmin.ToString());
            if (isSuperAdmin) throw new ForbiddenException("غير مسموح بتعديل الصلاحيات المباشرة للمالك الأساسي للنظام.");

            var repo = _unitOfWork.Repository<IdentityUserClaim<string>>();
            
            var existingUserClaims = await repo.Query()
                .Where(c => c.UserId == user.Id && c.ClaimType == "Permission")
                .ToListAsync();

            repo.RemoveRange(existingUserClaims);

            if (dto.SelectedPermissions != null && dto.SelectedPermissions.Any())
            {
                var newClaims = dto.SelectedPermissions.Select(p => new IdentityUserClaim<string>
                {
                    UserId = user.Id,
                    ClaimType = "Permission",
                    ClaimValue = p
                });
                
                await repo.CreateRangeAsync(newClaims);
            }

            await _unitOfWork.SaveAsync();

            var emailBody = EmailTemplates.BuildPermissionsChangedTemplate(user.FName);
            await _emailService.SendEmailAsync(user.Email!, "لقاء - تحديث الصلاحيات في منصة لقاء", emailBody);

            await _notificationService.SendNotificationAsync(new SendNotificationDTO
            {
                UserId = user.Id,
                Content = "قامت إدارة النظام بتحديث صلاحياتك الفردية (الاستثنائية).",
                Type = NotificationType.System
            });

            _logger.LogWarning($"New permissions assigned for User with ID: {dto.UserId}");
            return ApiResponse<string>.Ok(null, "تم تحديث الصلاحيات الخاصة بالمستخدم بنجاح.");
        }

        public async Task<ApiResponse<UserStatisticsDto>> GetUsersStatisticsAsync()
        {
            var now = DateTimeOffset.UtcNow;

            var baseQuery = _userManager.Users.Where(u => u.EmailConfirmed);

            var totalUsers = await baseQuery.CountAsync();
            var verifiedUsers = await baseQuery.CountAsync(u => u.VerificationStatus == VerificationStatus.Verified);
            var pendingUsers = await baseQuery.CountAsync(u => u.VerificationStatus == VerificationStatus.Pending);
            var bannedUsers = await baseQuery.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > now);

            var activeUsers = totalUsers - bannedUsers;
            var unverifiedUsers = totalUsers - verifiedUsers - pendingUsers;

            var statsDto = new UserStatisticsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                BannedUsers = bannedUsers,
                VerifiedUsers = verifiedUsers,
                PendingVerificationUsers = pendingUsers,
                UnverifiedUsers = unverifiedUsers
            };

            return ApiResponse<UserStatisticsDto>.Ok(statsDto, "تم جلب إحصائيات المستخدمين بنجاح.");
        }
    }
}