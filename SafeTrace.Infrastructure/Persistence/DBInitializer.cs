using SafeTrace.Application.Constants;
using SafeTrace.Application.Interfaces;

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

            string[] AdminPermissions = {Permissions.Dashboard.GetStatistics, Permissions.Dashboard.GetCasesStatistics,
                                         Permissions.Users.Reject, Permissions.Users.Approve, Permissions.Users.GetPermissions, Permissions.Users.AssignPermissions, Permissions.Users.GetAll, Permissions.Users.GetById, Permissions.Users.ToggleBlock, Permissions.Users.RegisterByAdmin, Permissions.Users.ChangeRole, Permissions.Users.GetUsersStatistics,
                                         Permissions.Roles.Delete, Permissions.Roles.Create, Permissions.Roles.GetPermissionsByRoleId, Permissions.Roles.UpdateRolePermissions,
                                         Permissions.Donations.GetDonations, Permissions.Donations.GetDonationStatistics,
                                         Permissions.Complaints.GetAll, Permissions.Complaints.GetById, Permissions.Complaints.MarkAsSolved, Permissions.Complaints.HardDelete, Permissions.Complaints.GetComplaintsStatistics,
                                         Permissions.Cases.GetAll,
                                         Permissions.UrgentCases.GetById, Permissions.UrgentCases.HardDelete, Permissions.UrgentCases.Reject, Permissions.UrgentCases.Approve, Permissions.UrgentCases.MarkAsFounded,
                                         Permissions.LongTermCases.GetById, Permissions.LongTermCases.HardDelete, Permissions.LongTermCases.Reject, Permissions.LongTermCases.Approve, Permissions.LongTermCases.MarkAsFounded, Permissions.LongTermCases.Create, Permissions.LongTermCases.Update, Permissions.LongTermCases.SoftDelete,
                                         Permissions.UnknownCases.GetById, Permissions.UnknownCases.HardDelete, Permissions.UnknownCases.Reject, Permissions.UnknownCases.Approve, Permissions.UnknownCases.MarkAsFounded, Permissions.UnknownCases.Create, Permissions.UnknownCases.Update, Permissions.UnknownCases.SoftDelete,
                                         Permissions.Chat.GetAll, Permissions.Chat.GetById, Permissions.Chat.GetMessages, Permissions.Chat.HardDelete, Permissions.Chat.DeleteMessageForEveryone, Permissions.Chat.GetChatStatistics,
                                         Permissions.AiMatching.Search};

            string[] ModeratorPermissions = {Permissions.Dashboard.GetCasesStatistics,
                                             Permissions.Users.Reject, Permissions.Users.Approve, Permissions.Users.GetAll, Permissions.Users.GetById, Permissions.Users.ToggleBlock, Permissions.Users.GetUsersStatistics,
                                             Permissions.Complaints.GetAll, Permissions.Complaints.GetById, Permissions.Complaints.MarkAsSolved, Permissions.Complaints.GetComplaintsStatistics,
                                             Permissions.Cases.GetAll,
                                             Permissions.UrgentCases.GetById, Permissions.UrgentCases.Reject, Permissions.UrgentCases.Approve, Permissions.UrgentCases.MarkAsFounded,
                                             Permissions.LongTermCases.GetById, Permissions.LongTermCases.Reject, Permissions.LongTermCases.Approve, Permissions.LongTermCases.MarkAsFounded, Permissions.LongTermCases.Create, Permissions.LongTermCases.Update, Permissions.LongTermCases.SoftDelete,
                                             Permissions.UnknownCases.GetById, Permissions.UnknownCases.Reject, Permissions.UnknownCases.Approve, Permissions.UnknownCases.MarkAsFounded, Permissions.UnknownCases.Create, Permissions.UnknownCases.Update, Permissions.UnknownCases.SoftDelete,
                                             Permissions.Chat.GetById, Permissions.Chat.GetMessages, Permissions.Chat.DeleteMessageForEveryone,
                                             Permissions.AiMatching.Search};

            string[] VerifiedUserPermissions = {Permissions.UrgentCases.MarkAsFounded,
                                                Permissions.LongTermCases.MarkAsFounded, Permissions.LongTermCases.Create, Permissions.LongTermCases.Update, Permissions.LongTermCases.SoftDelete,
                                                Permissions.UnknownCases.MarkAsFounded, Permissions.UnknownCases.Create, Permissions.UnknownCases.Update, Permissions.UnknownCases.SoftDelete,
                                                Permissions.Chat.GetById, Permissions.Chat.GetMessages, Permissions.Chat.DeleteMessageForEveryone,
                                                Permissions.AiMatching.Search};

            string[] UserPermissions = {Permissions.UrgentCases.MarkAsFounded,
                                        Permissions.Chat.GetById, Permissions.Chat.GetMessages, Permissions.Chat.DeleteMessageForEveryone,
                                        Permissions.AiMatching.Search};

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