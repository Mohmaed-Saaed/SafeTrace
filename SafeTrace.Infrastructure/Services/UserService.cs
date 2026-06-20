using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using System.Reflection;
using System.Security.Claims;

namespace SafeTrace.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
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

        public async Task<ApiResponse<GetUserDto>> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new NotFoundException($"User account with ID '{userId}' was not found.");

            var userDto = _mapper.Map<GetUserDto>(user);

            return ApiResponse<GetUserDto>.Ok(userDto);
        }

        public async Task<ApiResponse<string>> ChangeUserRoleAsync(string userId, ChangeUserRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User account not found in the system.");

            var roleExists = await _roleManager.RoleExistsAsync(dto.NewRole);
            if (!roleExists) throw new BadRequestException("The specified role does not exist.");

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new BadRequestException("Failed to remove existing roles.");

            var addResult = await _userManager.AddToRoleAsync(user, dto.NewRole);
            if (!addResult.Succeeded) throw new BadRequestException("Failed to assign the new role.");

            return ApiResponse<string>.Ok(user.Id, "User role has been successfully updated.");
        }

        public async Task<ApiResponse<UserPermissionsResponseDto>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User account not found.");

            var existingClaims = await _userManager.GetClaimsAsync(user);
            var assignedPermissions = existingClaims
                .Where(c => c.Type == "Permission")
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

        public async Task<ApiResponse<string>> AssignUserPermissionsAsync(string userId, AssignUserPermissionsDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
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

            return ApiResponse<string>.Ok(user.Id, "User specific permissions updated successfully.");
        }
    }
}