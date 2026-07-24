using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.RolePermission.Request;
using SafeTrace.Application.DTOs.RolePermission.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Infrastructure.Services
{
    public class RolePermissionService : IRolePermissionService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RolePermissionService> _logger;

        public RolePermissionService(
            RoleManager<IdentityRole> roleManager, 
            ILogger<RolePermissionService> logger, 
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _roleManager = roleManager;
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name!
                }).ToListAsync();

            return ApiResponse<List<RoleDto>>.Ok(roles);
        }

        public async Task<ApiResponse<string>> CreateRoleAsync(CreateRoleDto dto)
        {
            var roleExists = await _roleManager.RoleExistsAsync(dto.RoleName);
            if (roleExists) throw new ConflictException("هذا الدور (Role) موجود بالفعل في النظام.");

            var role = new IdentityRole(dto.RoleName);
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create role {RoleName}. Errors: {Errors}", dto.RoleName, errors);
                throw new BadRequestException("حدث خطأ غير متوقع أثناء محاولة إنشاء الدور.");
            }

            _logger.LogInformation("A new role {RoleName} has been created successfully.", dto.RoleName);

            return ApiResponse<string>.Ok(null, "تم إنشاء الدور بنجاح.");
        }

        public async Task<ApiResponse<string>> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) throw new NotFoundException("هذا الدور غير موجود.");

            var coreRoles = new List<string> { UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString(), UserRole.User.ToString(), UserRole.VerifiedUser.ToString(), UserRole.Moderator.ToString() };
            if (coreRoles.Contains(role.Name!)) throw new ForbiddenException("لا يمكن حذف الأدوار الأساسية للنظام.");

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Any()) throw new ConflictException($"لا يمكن حذف هذا الدور لوجود {usersInRole.Count} مستخدم مرتبط به. يرجى تغيير أدوارهم أولاً.");

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to delete role {RoleName}. Errors: {Errors}", role.Name, errors);
                throw new BadRequestException("حدث خطأ غير متوقع أثناء محاولة حذف الدور.");
            }

            _logger.LogInformation("Role {RoleName} has been deleted successfully.", role.Name);

            return ApiResponse<string>.Ok(null, "تم حذف الدور بنجاح.");
        }

        public async Task<ApiResponse<RolePermissionsResponseDto>> GetPermissionsByRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) throw new NotFoundException("لم يتم العثور على هذا الدور (Role).");

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var assignedPermissions = existingClaims.Where(c => c.Type == "Permission")
                                                    .Select(c => c.Value)
                                                    .ToList();

            var allPermissions = Application.Constants.Permissions.GetAllPermissions();

            var response = new RolePermissionsResponseDto
            {
                RoleId = role.Id,
                RoleName = role.Name!,
                Permissions = allPermissions.Select(p => new RolePermissionDto
                {
                    PermissionValue = p,
                    IsSelected = assignedPermissions.Contains(p)
                }).ToList()
            };

            return ApiResponse<RolePermissionsResponseDto>.Ok(response);
        }

        public async Task<ApiResponse<string>> UpdateRolePermissionsAsync(UpdateRolePermissionsDto dto)
        {
            var role = await _roleManager.FindByIdAsync(dto.RoleId);
            if (role == null) throw new NotFoundException("لم يتم العثور على هذا الدور (Role).");

            if (role.Name == UserRole.SuperAdmin.ToString()) 
                throw new ForbiddenException("لأسباب أمنية، لا يمكن تعديل صلاحيات دور المالك الاساسي للنظام");

            var repo = _unitOfWork.Repository<IdentityRoleClaim<string>>();
            
            var existingRoleClaims = await repo.Query()
                .Where(c => c.RoleId == role.Id && c.ClaimType == "Permission")
                .ToListAsync();

            repo.RemoveRange(existingRoleClaims);

            if (dto.SelectedPermissions != null && dto.SelectedPermissions.Any())
            {
                var newClaims = dto.SelectedPermissions.Select(p => new IdentityRoleClaim<string>
                {
                    RoleId = role.Id,
                    ClaimType = "Permission",
                    ClaimValue = p
                });
                
                await repo.CreateRangeAsync(newClaims);
            }

            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(role.Id, "تم تحديث صلاحيات الدور بنجاح.");
        }
    }
}