using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.Interfaces;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System.Security.Claims;

namespace SafeTrace.Infrastructure.Persistence
{
    public class DBInitializer : IDBInitializer
    {
        public readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public DBInitializer(IUnitOfWork unitOfWork, 
                             RoleManager<IdentityRole> roleManager,
                             UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task Initialize()
        {
            string[] Roles = { "User", "Admin", "VerifiedUser" };

            foreach (string role in Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var Role = new IdentityRole(role);
                    await _roleManager.CreateAsync(Role);
                }
            }

            var adminEmail = "esraataha3092001@gmail.com";
            var user = await _userManager.FindByEmailAsync(adminEmail);
            if(user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            string[] AdminPermissions = { "UrgentCases.GetAll", "UrgentCases.GetById", "UrgentCases.GetMyCases", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.HardDelete", "UrgentCases.Reject", "UrgentCases.Approve", "UrgentCases.MarkAsFounded",
                                          "UnknownCases.GetAll", "UnknownCases.GetById", "UnknownCases.GetMyCases", "UnknownCases.Create", "UnknownCases.Update", "UnknownCases.SoftDelete", "UnknownCases.HardDelete", "UnknownCases.Reject", "UnknownCases.Approve", "UnknownCases.MarkAsFounded",
                                          "LongTermCases.GetAll", "LongTermCases.GetById", "LongTermCases.GetMyCases", "LongTermCases.Create", "LongTermCases.Update", "LongTermCases.SoftDelete", "LongTermCases.HardDelete", "LongTermCases.Reject", "LongTermCases.Approve", "LongTermCases.MarkAsFounded",
                                          "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                          "Profile.GetUserInfo", "Profile.UpdateUserInfo",
                                          "Complaints.GetAll", "Complaints.GetById", "Complaints.Create", "Complaints.HardDelete", "Complaints.MarkAsSolved",
                                          "Account.ChangePassword",
                                          "AiMatching.Search",
                                          "Roles.GetAll", "Roles.Create", "Roles.Delete", "Roles.GetPermissionsByRoleId", "Roles.UpdateRolePermissions",
                                          "Users.GetAll", "Users.GetById", "Users.RegisterByAdmin",  "Users.ChangeRole", "Users.GetPermissions", "Users.AssignPermissions", "Users.Approve", "Users.Reject", "Users.ToggleBlock",
                                          "Chat.GetAll", "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.MarkAsRead", "Chat.HardDelete", "Chat.SoftDelete", "Chat.DeleteMessage", "Chat.DeleteMessageForEveryone",
                                          "Dashboard.GetStatistics"};


            string[] VerifiedUserPermissions = { "UrgentCases.GetById", "UrgentCases.GetMyCases", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.MarkAsFounded",
                                                 "UnknownCases.GetById", "UnknownCases.GetMyCases", "UnknownCases.Create", "UnknownCases.Update", "UnknownCases.SoftDelete", "UnknownCases.MarkAsFounded",
                                                 "LongTermCases.GetById", "LongTermCases.GetMyCases", "LongTermCases.Create", "LongTermCases.Update", "LongTermCases.SoftDelete", "LongTermCases.MarkAsFounded",
                                                 "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                                 "Profile.GetUserInfo", "Profile.UpdateUserInfo",
                                                 "Complaints.Create",
                                                 "Account.ChangePassword",
                                                 "AiMatching.Search",
                                                 "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.MarkAsRead", "Chat.SoftDelete", "Chat.DeleteMessage"};

            string[] UserPermissions = { "UrgentCases.GetById", "UrgentCases.GetMyCases", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.MarkAsFounded",
                                         "UnknownCases.GetMyCases",
                                         "LongTermCases.GetMyCases",
                                         "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                         "Profile.GetUserInfo", "Profile.UpdateUserInfo",
                                         "Complaints.Create",
                                         "Account.ChangePassword",
                                         "AiMatching.Search",
                                         "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.MarkAsRead", "Chat.SoftDelete", "Chat.DeleteMessage"};

            await AssignPermissionsToRoleAsync("Admin", AdminPermissions);
            await AssignPermissionsToRoleAsync("VerifiedUser", VerifiedUserPermissions);
            await AssignPermissionsToRoleAsync("User", UserPermissions);
        }

        private async Task AssignPermissionsToRoleAsync(string roleName, string[] permissions)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var currentClaims = await _roleManager.GetClaimsAsync(role);
                var currentPermissions = currentClaims.Select(c => c.Value).ToList();

                foreach (var permission in permissions)
                {
                    if (!currentPermissions.Contains(permission))
                    {
                        await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                    }
                }
            }
        }
    }
}