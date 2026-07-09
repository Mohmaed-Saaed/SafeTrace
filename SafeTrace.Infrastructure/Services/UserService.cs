using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using System.Reflection;
using System.Security.Claims;

namespace SafeTrace.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            ApplicationDbContext context,
            IMapper mapper,
            IFileStorageService fileStorageService,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _context = context;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<ApiResponse<PaginationResponseDto<GetUserDto>>> GetAllUsersAsync(UserFilterDto filterDto)
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filterDto.SearchTerm))
            {
                var term = filterDto.SearchTerm.Trim().ToLower();
                query = query.Where(u => u.FName.ToLower().Contains(term) ||
                                         u.LName.ToLower().Contains(term) ||
                                         u.Email!.ToLower().Contains(term) ||
                                         u.PhoneNumber!.Contains(term));
            }

            if (filterDto.VerificationStatus.HasValue)
            {
                query = query.Where(u => u.VerificationStatus == filterDto.VerificationStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(filterDto.RoleId))
            {
                var userIdsInRole = _context.UserRoles
                    .Where(ur => ur.RoleId == filterDto.RoleId)
                    .Select(ur => ur.UserId);

                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            var totalCount = await query.CountAsync();

            var users = await query.Skip((filterDto.PageNumber - 1) * filterDto.PageSize)
                                   .Take(filterDto.PageSize)
                                   .ToListAsync();

            var userDtos = _mapper.Map<List<GetUserDto>>(users);

            foreach (var dto in userDtos)
            {
                var userEntity = users.First(u => u.Id == dto.Id);
                var roles = await _userManager.GetRolesAsync(userEntity);
                dto.Role = roles.FirstOrDefault()!;
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
            userDto.Role = roles.FirstOrDefault()!;

            return ApiResponse<GetUserByIdDto>.Ok(userDto);
        }

        public async Task<ApiResponse<string>> ChangeUserRoleAsync(string currentUserId, ChangeUserRoleDto dto)
        {
            if (currentUserId == dto.UserId) throw new BadRequestException("لا يمكنك تعديل الصلاحيات أو الدور لحسابك الشخصي.");

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) throw new NotFoundException("لم يتم العثور على هذا الحساب في النظام.");

            if (user.Email == SystemConstants.RootAdminEmail) throw new ForbiddenException("غير مسموح بالمساس بصلاحيات أو دور المالك الأساسي للنظام.");

            var roleExists = await _roleManager.RoleExistsAsync(dto.NewRole);
            if (!roleExists) throw new BadRequestException("الدور (Role) المحدد غير موجود.");

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new BadRequestException("فشل في إزالة الأدوار الحالية للمستخدم.");

            var addResult = await _userManager.AddToRoleAsync(user, dto.NewRole);
            if (!addResult.Succeeded) throw new BadRequestException("فشل في تعيين الدور الجديد للمستخدم.");

            _logger.LogWarning($"Role changed for User with ID: {dto.UserId} from {string.Join(",", currentRoles)} to {dto.NewRole}");
            return ApiResponse<string>.Ok(null, "تم تحديث دور المستخدم بنجاح.");
        }

        public async Task<ApiResponse<string>> RegisterByAdminAsync(RegisterByAdminDto dto)
        {
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
                    VerificationStatus = VerificationStatus.Verified
                };

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to register user {Email} by Admin. Errors: {Errors}", dto.Email, errors);
                    throw new BadRequestException("فشلت عملية إنشاء الحساب. تأكد من استيفاء كلمة المرور للشروط.");
                }

                await _userManager.AddToRoleAsync(user, dto.Role);

                await _unitOfWork.CommitTransactionAsync();

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

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new BadRequestException("فشل في إزالة الدور الحالي للمستخدم.");

            var addResult = await _userManager.AddToRoleAsync(user, "VerifiedUser");
            if (!addResult.Succeeded) throw new BadRequestException("فشل في ترقية حساب المستخدم إلى 'مستخدم موثق'.");

            user.VerificationStatus = VerificationStatus.Verified;
            await _userManager.UpdateAsync(user);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to approve user verification for UserId: {UserId}. Errors: {Errors}",
                userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));

                throw new BadRequestException("تعذر الموافقة على طلب توثيق المستخدم. يرجى المحاولة مرة أخرى.");
            }

            _logger.LogWarning($"Role changed for User with ID: {userId} from {string.Join(",", currentRoles)} to VerifiedUser");
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

            user.VerificationStatus = VerificationStatus.Unverified;
            await _userManager.UpdateAsync(user);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to reject user verification for UserId: {UserId}. Errors: {Errors}",
                userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));

                throw new BadRequestException("تعذر رفض طلب توثيق المستخدم. يرجى المحاولة مرة أخرى.");
            }

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

            if ((targetUserRoles.Contains("Admin") || targetUserRoles.Contains("Moderator")) && currentUserRoles.Contains("Moderator"))
            {
                throw new ForbiddenException("غير مسموح للمشرف (Moderator) بحظر أو فك حظر مديري النظام (Admins) أو المشرفين الأخرين.");
            }

            bool isCurrentlyBlocked = targetUser.LockoutEnd.HasValue && targetUser.LockoutEnd.Value > DateTimeOffset.UtcNow;

            if (isCurrentlyBlocked)
            {
                await _userManager.SetLockoutEndDateAsync(targetUser, null);

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

            var allPermissions = new List<string>();
            var modules = typeof(Application.Constants.Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

            foreach (var module in modules)
            {
                var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                foreach (var field in fields)
                {
                    var value = field.GetValue(null)?.ToString();
                    if (value != null) allPermissions.Add(value);
                }
            }

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

            if (user.Email == SystemConstants.RootAdminEmail) throw new ForbiddenException("غير مسموح بتعديل الصلاحيات المباشرة للمالك الأساسي للنظام.");

            var existingClaims = await _userManager.GetClaimsAsync(user);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission");

            foreach (var claim in permissionClaims)
            {
                var removeResult = await _userManager.RemoveClaimAsync(user, claim);
                if (!removeResult.Succeeded) throw new BadRequestException("فشل في مسح الصلاحيات الحالية للمستخدم.");
            }

            foreach (var permission in dto.SelectedPermissions)
            {
                var addResult = await _userManager.AddClaimAsync(user, new Claim("Permission", permission));
                if (!addResult.Succeeded) throw new BadRequestException("فشل في تعيين الصلاحيات الجديدة للمستخدم.");
            }

            _logger.LogWarning($"New permissions assigned for User with ID: {dto.UserId}");
            return ApiResponse<string>.Ok(null, "تم تحديث الصلاحيات الخاصة بالمستخدم بنجاح.");
        }
    }
}