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
        public DBInitializer(IUnitOfWork unitOfWork, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
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

            string[] AdminPermissions = { "UrgentCases.GetAll", "UrgentCases.GetById", "UrgentCases.GetMyCases", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.HardDelete", "UrgentCases.Reject", "UrgentCases.Approve", "UrgentCases.MarkAsFounded",
                                         "UnkownCases.GetAll", "UnkownCases.GetById", "UnkownCases.GetMyCases", "UnkownCases.Create", "UnkownCases.Update", "UnkownCases.SoftDelete", "UnkownCases.HardDelete", "UnkownCases.Reject", "UnkownCases.Approve", "UnkownCases.MarkAsFounded",
                                         "LongTermCases.GetAll", "LongTermCases.GetById", "LongTermCases.GetMyCases", "LongTermCases.Create", "LongTermCases.Update", "LongTermCases.SoftDelete", "LongTermCases.HardDelete", "LongTermCases.Reject", "LongTermCases.Approve", "LongTermCases.MarkAsFounded",
                                         "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                         "Profile.GetUserInfo", "Profile.UpdateUserInfo",
                                         "Complaints.GetAll", "Complaints.GetById", "Complaints.Create", "Complaints.HardDelete", "Complaints.MarkAsSolved",
                                         "RolePermission.GetAllRoles", "RolePermission.GetPermissionsByRole", "RolePermission.UpdateRolePermissions",
                                         "Users.GetAll", "Users.GetById",  "Users.ChangeRole", "Users.GetPermissions", "Users.AssignPermissions", "Users.Approve", "Users.Reject", "Users.ToggleBlock",
                                         "Chat.GetAll", "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.MarkAsRead",
                                         "Dashboard.GetStatistics"};


            string[] VerifiedUserPermissions = { "UrgentCases.GetById", "UrgentCases.GetMyCases", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.MarkAsFounded",
                                                 "UnkownCases.GetById", "UnkownCases.GetMyCases", "UnkownCases.Create", "UnkownCases.Update", "UnkownCases.SoftDelete", "UnkownCases.MarkAsFounded",
                                                 "LongTermCases.GetById", "LongTermCases.GetMyCases", "LongTermCases.Create", "LongTermCases.Update", "LongTermCases.SoftDelete", "LongTermCases.MarkAsFounded",
                                                 "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                                 "Profile.GetUserInfo", "Profile.UpdateUserInfo",
                                                 "Complaints.Create",
                                                 "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.MarkAsRead"};

            string[] UserPermissions = { "UrgentCases.GetById", "UrgentCases.GetMyCases", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.MarkAsFounded",
                                         "UnkownCases.GetMyCases",
                                         "LongTermCases.GetMyCases",
                                         "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                         "Profile.GetUserInfo", "Profile.UpdateUserInfo",
                                         "Complaints.Create",
                                         "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.MarkAsRead"};

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