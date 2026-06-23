using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System.Reflection;
using System.Security.Claims;

namespace SafeTrace.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorageService,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
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

            var totalCount = await query.CountAsync();

            var users = await query.Skip((filterDto.PageNumber - 1) * filterDto.PageSize)
                                   .Take(filterDto.PageSize)
                                   .ProjectTo<GetUserDto>(_mapper.ConfigurationProvider)
                                   .ToListAsync();

            var paginatedResult = new PaginationResponseDto<GetUserDto>
            {
                Items = users,
                PageNumber = filterDto.PageNumber,
                PageSize = filterDto.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse<PaginationResponseDto<GetUserDto>>.Ok(paginatedResult, "Users retrieved successfully with current filter criteria.");
        }

        public async Task<ApiResponse<GetUserByIdDto>> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) throw new NotFoundException("User account not found in the system.");

            var userDto = _mapper.Map<GetUserByIdDto>(user);

            return ApiResponse<GetUserByIdDto>.Ok(userDto);
        }

        public async Task<ApiResponse<string>> ChangeUserRoleAsync(ChangeUserRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) throw new NotFoundException("User account not found in the system.");

            var roleExists = await _roleManager.RoleExistsAsync(dto.NewRole);
            if (!roleExists) throw new BadRequestException("The specified role does not exist.");

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new BadRequestException("Failed to remove existing roles.");

            var addResult = await _userManager.AddToRoleAsync(user, dto.NewRole);
            if (!addResult.Succeeded) throw new BadRequestException("Failed to assign the new role.");

            _logger.LogWarning($"Role changed for User with ID: {dto.UserId} from {string.Join(",", currentRoles)} to {dto.NewRole}");
            return ApiResponse<string>.Ok(null, "User role has been successfully updated.");
        }

        public async Task<ApiResponse<string>> ApproveUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User account not found in the system.");

            if (user.IdentificationImage == null) throw new BadRequestException("There is no identification image for this user to approve.");

            user.VerificationStatus = VerificationStatus.Verified;
            
            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new BadRequestException("Failed to remove existing roles to this user.");

            var addResult = await _userManager.AddToRoleAsync(user, "VerifiedUser");
            if (!addResult.Succeeded) throw new BadRequestException("Failed to assign the new role to this user.");

            _logger.LogWarning($"Role changed for User with ID: {userId} from {string.Join(",", currentRoles)} to VerifiedUser");
            return ApiResponse<string>.Ok(null, "User Approved successfully");
        }

        public async Task<ApiResponse<string>> RejectUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User account not found in the system.");

            if (user.IdentificationImage == null) throw new BadRequestException("There is no identification image for this user to reject.");

            user.VerificationStatus = VerificationStatus.Unverified;

            var DeletedResult = _fileStorageService.DeleteFile(user.IdentificationImage);
            if (!DeletedResult) throw new BadRequestException("Failed to remove IdentificationImage for this user.");

            return ApiResponse<string>.Ok(null, "User Rejected successfully");
        }

        public async Task<ApiResponse<string>> ToggleUserBlockStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User account not found.");

            bool isCurrentlyBlocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            if (isCurrentlyBlocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);

                _logger.LogInformation("User {Email} has been unblocked by Admin.", user.Email);
                return ApiResponse<string>.Ok(user.Id, "User account has been successfully unblocked.");
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

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

                _logger.LogInformation("User {Email} has been blocked and all active sessions revoked.", user.Email);
                return ApiResponse<string>.Ok(null, "User has been blocked and all active sessions terminated.");
            }
        }

        public async Task<ApiResponse<UserPermissionsResponseDto>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User account not found.");

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

        public async Task<ApiResponse<string>> AssignUserPermissionsAsync(AssignUserPermissionsDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) throw new NotFoundException("User account not found.");

            var existingClaims = await _userManager.GetClaimsAsync(user);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission");

            foreach (var claim in permissionClaims)
            {
                var removeResult = await _userManager.RemoveClaimAsync(user, claim);
                if (!removeResult.Succeeded) throw new BadRequestException("Failed to clear current permission policies.");
            }

            foreach (var permission in dto.SelectedPermissions)
            {
                var addResult = await _userManager.AddClaimAsync(user, new Claim("Permission", permission));
                if (!addResult.Succeeded) throw new BadRequestException("Failed to assign the new permission policies.");
            }

            _logger.LogWarning($"New permissions assigned for User with ID: {dto.UserId}");
            return ApiResponse<string>.Ok(null, "User specific permissions updated successfully.");
        }
    }
}