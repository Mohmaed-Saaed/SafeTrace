using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SafeTrace.Infrastructure.DataAccess;
using System.Security.Claims;

namespace SafeTrace.Infrastructure.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public PermissionHandler(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User == null) return;

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            var userClaims = await _userManager.GetClaimsAsync(user);
            var hasUserPermission = userClaims.Any(c => c.Type == "Permission" && c.Value == requirement.Permission);
            if (hasUserPermission)
            {
                context.Succeed(requirement);
                return;
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            
            if (userRole != null)
            {
                var role = await _roleManager.FindByNameAsync(userRole);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    if (roleClaims.Any(c => c.Type == "Permission" && c.Value == requirement.Permission))
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }
            }
        }
    }
}