using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using SafeTrace.Infrastructure.DataAccess;
using System.Security.Claims;

namespace SafeTrace.Infrastructure.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public PermissionHandler(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _cache = cache;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User == null) return;

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var cacheKey = $"UserPermissions_{userId}";

            if (!_cache.TryGetValue(cacheKey, out HashSet<string>? userPermissions))
            {
                userPermissions = new HashSet<string>();

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var userClaims = await _userManager.GetClaimsAsync(user);
                    foreach (var claim in userClaims.Where(c => c.Type == "Permission"))
                    {
                        userPermissions.Add(claim.Value);
                    }

                    var userRoles = await _userManager.GetRolesAsync(user);
                    var userRole = userRoles.FirstOrDefault();
                    
                    if (userRole != null)
                    {
                        var role = await _roleManager.FindByNameAsync(userRole);
                        if (role != null)
                        {
                            var roleClaims = await _roleManager.GetClaimsAsync(role);
                            foreach (var claim in roleClaims.Where(c => c.Type == "Permission"))
                            {
                                userPermissions.Add(claim.Value);
                            }
                        }
                    }
                }

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
                _cache.Set(cacheKey, userPermissions, cacheOptions);
            }

            if (userPermissions != null && userPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }
}