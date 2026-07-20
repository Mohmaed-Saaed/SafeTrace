using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.Constants;
using SafeTrace.Application.Interfaces;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
            string[] Roles = { "User", "Admin", "VerifiedUser", "Moderator" };

            foreach (string role in Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var Role = new IdentityRole(role);
                    await _roleManager.CreateAsync(Role);
                } 
            }

            var adminEmail = SystemConstants.RootAdminEmail;
            var user = await _userManager.FindByEmailAsync(adminEmail);
            if(user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            string[] AdminPermissions = { "UrgentCases.GetAll", "UrgentCases.GetById", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.HardDelete", "UrgentCases.Reject", "UrgentCases.Approve", "UrgentCases.MarkAsFounded",
                                          "UnknownCases.GetAll", "UnknownCases.GetById", "UnknownCases.Create", "UnknownCases.Update", "UnknownCases.SoftDelete", "UnknownCases.HardDelete", "UnknownCases.Reject", "UnknownCases.Approve", "UnknownCases.MarkAsFounded",
                                          "LongTermCases.GetAll", "LongTermCases.GetById", "LongTermCases.Create", "LongTermCases.Update", "LongTermCases.SoftDelete", "LongTermCases.HardDelete", "LongTermCases.Reject", "LongTermCases.Approve", "LongTermCases.MarkAsFounded",
                                          "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                          "Profile.GetUserInfo", "Profile.UpdateUserInfo", "Profile.UpdateName", "Profile.UpdateHomeLocation", "Profile.UpdateProfileImage", "Profile.UpdateIdImage", "Profile.GetVisitedUserInfo", "Profile.UpdatePhoneNumber", "Profile.GetMyCases",
                                          "Complaints.GetAll", "Complaints.GetById", "Complaints.Create", "Complaints.HardDelete", "Complaints.MarkAsSolved", "Complaints.GetComplaintsStatistics",
                                          "Account.ChangePassword",
                                          "AiMatching.Search",
                                          "Roles.GetAll", "Roles.Create", "Roles.Delete", "Roles.GetPermissionsByRoleId", "Roles.UpdateRolePermissions",
                                          "Users.GetAll", "Users.GetById", "Users.RegisterByAdmin",  "Users.ChangeRole", "Users.GetPermissions", "Users.AssignPermissions", "Users.Approve", "Users.Reject", "Users.ToggleBlock", "Users.GetUsersStatistics",
                                          "Chat.GetAll", "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.StartContext", "Chat.MarkAsRead", "Chat.HardDelete", "Chat.SoftDelete", "Chat.DeleteMessage", "Chat.DeleteMessageForEveryone", "Chat.GetChatStatistics",
                                          "Donations.GetDonations", "Donations.GetMyDonations",
                                          "Dashboard.GetStatistics", "Dashboard.GetCasesStatistics"};

            string[] ModeratorPermissions = { "UrgentCases.GetAll", "UrgentCases.GetById", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.Reject", "UrgentCases.Approve", "UrgentCases.MarkAsFounded",
                                              "UnknownCases.GetAll", "UnknownCases.GetById", "UnknownCases.Create", "UnknownCases.Update", "UnknownCases.SoftDelete", "UnknownCases.Reject", "UnknownCases.Approve", "UnknownCases.MarkAsFounded",
                                              "LongTermCases.GetAll", "LongTermCases.GetById", "LongTermCases.Create", "LongTermCases.Update", "LongTermCases.SoftDelete", "LongTermCases.Reject", "LongTermCases.Approve", "LongTermCases.MarkAsFounded",
                                              "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                              "Profile.GetUserInfo", "Profile.UpdateUserInfo", "Profile.UpdateName", "Profile.UpdateHomeLocation", "Profile.UpdateProfileImage", "Profile.UpdateIdImage", "Profile.GetVisitedUserInfo", "Profile.UpdatePhoneNumber", "Profile.GetMyCases",
                                              "Complaints.GetAll", "Complaints.GetById", "Complaints.Create", "Complaints.MarkAsSolved",
                                              "Account.ChangePassword",
                                              "AiMatching.Search",
                                              "Users.GetAll", "Users.GetById", "Users.Approve", "Users.Reject", "Users.ToggleBlock", "Users.GetUsersStatistics",
                                              "Roles.GetAll",
                                              "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.StartContext", "Chat.MarkAsRead", "Chat.SoftDelete", "Chat.DeleteMessage", "Chat.DeleteMessageForEveryone",
                                              "Donations.GetMyDonations",
                                              "Dashboard.GetStatistics", "Dashboard.GetCasesStatistics"};

            string[] VerifiedUserPermissions = { "UrgentCases.GetById", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.MarkAsFounded",
                                                 "UnknownCases.GetById", "UnknownCases.Create", "UnknownCases.Update", "UnknownCases.SoftDelete", "UnknownCases.MarkAsFounded",
                                                 "LongTermCases.GetById", "LongTermCases.Create", "LongTermCases.Update", "LongTermCases.SoftDelete", "LongTermCases.MarkAsFounded",
                                                 "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                                 "Profile.GetUserInfo", "Profile.UpdateUserInfo", "Profile.UpdateName", "Profile.UpdateHomeLocation", "Profile.UpdateProfileImage", "Profile.UpdateIdImage", "Profile.GetVisitedUserInfo", "Profile.UpdatePhoneNumber", "Profile.GetMyCases",
                                                 "Complaints.Create",
                                                 "Account.ChangePassword",
                                                 "AiMatching.Search",
                                                 "Donations.GetMyDonations",
                                                 "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.StartContext", "Chat.MarkAsRead", "Chat.SoftDelete", "Chat.DeleteMessage", "Chat.DeleteMessageForEveryone"};

            string[] UserPermissions = { "UrgentCases.GetById", "UrgentCases.Create", "UrgentCases.Update", "UrgentCases.SoftDelete", "UrgentCases.MarkAsFounded",
                                         "Notifications.GetMyNotifications", "Notifications.DeleteNotification", "Notifications.MarkAsRead", "Notifications.MarkAllAsRead",
                                         "Profile.GetUserInfo", "Profile.UpdateUserInfo", "Profile.UpdateName", "Profile.UpdateHomeLocation", "Profile.UpdateProfileImage", "Profile.UpdateIdImage", "Profile.GetVisitedUserInfo", "Profile.UpdatePhoneNumber", "Profile.GetMyCases",
                                         "Complaints.Create",
                                         "Account.ChangePassword",
                                         "AiMatching.Search",
                                         "Donations.GetMyDonations",
                                         "Chat.GetById", "Chat.GetMyChats",  "Chat.GetMessages", "Chat.SendMessage", "Chat.Create", "Chat.StartContext", "Chat.MarkAsRead", "Chat.SoftDelete", "Chat.DeleteMessage", "Chat.DeleteMessageForEveryone"};

            await AssignPermissionsToRoleAsync("Admin", AdminPermissions);
            await AssignPermissionsToRoleAsync("Moderator", ModeratorPermissions);
            await AssignPermissionsToRoleAsync("VerifiedUser", VerifiedUserPermissions);
            await AssignPermissionsToRoleAsync("User", UserPermissions);
        }

        private async Task AssignPermissionsToRoleAsync(string roleName, string[] permissions)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var repo = _unitOfWork.Repository<IdentityRoleClaim<string>>();
                var currentClaims = await repo.Query().Where(c => c.RoleId == role.Id && c.ClaimType == "Permission").ToListAsync();
                var currentPermissions = currentClaims.Select(c => c.ClaimValue).ToList();

                var newClaims = permissions.Where(p => !currentPermissions.Contains(p))
                    .Select(p => new IdentityRoleClaim<string>
                    {
                        RoleId = role.Id,
                        ClaimType = "Permission",
                        ClaimValue = p
                    }).ToList();

                if (newClaims.Any())
                {
                    await repo.CreateRangeAsync(newClaims);
                    await _unitOfWork.SaveAsync();
                }
            }
        }
    }
}