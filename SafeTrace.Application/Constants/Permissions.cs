namespace SafeTrace.Application.Constants
{
    public static class Permissions
    {
        public static class Cases
        {
            public const string GetAll = "Cases.GetAll";
        }

        public static class UrgentCases
        {
            public const string GetById = "UrgentCases.GetById";
            public const string HardDelete = "UrgentCases.HardDelete";
            public const string Reject = "UrgentCases.Reject";
            public const string Approve = "UrgentCases.Approve";
            public const string MarkAsFounded = "UrgentCases.MarkAsFounded";
        }

        public static class LongTermCases
        {
            public const string GetById = "LongTermCases.GetById";
            public const string Create = "LongTermCases.Create";
            public const string Update = "LongTermCases.Update";
            public const string SoftDelete = "LongTermCases.SoftDelete";
            public const string HardDelete = "LongTermCases.HardDelete";
            public const string Reject = "LongTermCases.Reject";
            public const string Approve = "LongTermCases.Approve";
            public const string MarkAsFounded = "LongTermCases.MarkAsFounded";
        }

        public static class UnknownCases
        {
            public const string GetById = "UnknownCases.GetById";
            public const string Create = "UnknownCases.Create";
            public const string Update = "UnknownCases.Update";
            public const string SoftDelete = "UnknownCases.SoftDelete";
            public const string HardDelete = "UnknownCases.HardDelete";
            public const string Reject = "UnknownCases.Reject";
            public const string Approve = "UnknownCases.Approve";
            public const string MarkAsFounded = "UnknownCases.MarkAsFounded";
        }

        public static class Dashboard
        {
            public const string GetStatistics = "Dashboard.GetStatistics";
            public const string GetCasesStatistics = "Dashboard.GetCasesStatistics";
            public const string GetAuditLogs = "Dashboard.GetAuditLogs";
        }

        public static class AiMatching
        {
            public const string Search = "AiMatching.Search";
        }

        public static class Complaints
        {
            public const string GetAll = "Complaints.GetAll";
            public const string GetById = "Complaints.GetById";
            public const string HardDelete = "Complaints.HardDelete";
            public const string MarkAsSolved = "Complaints.MarkAsSolved";
            public const string GetComplaintsStatistics = "Complaints.GetComplaintsStatistics";
        }

        public static class Donations
        {
            public const string GetDonations = "Donations.GetDonations";
            public const string GetDonationStatistics = "Donations.GetDonationStatistics";
        }

        public static class Roles
        {
            public const string Create = "Roles.Create";
            public const string Delete = "Roles.Delete";
            public const string GetPermissionsByRoleId = "Roles.GetPermissionsByRoleId";
            public const string UpdateRolePermissions = "Roles.UpdateRolePermissions";
        }

        public static class Users
        {
            public const string GetAll = "Users.GetAll";
            public const string GetById = "Users.GetById";
            public const string RegisterByAdmin = "Users.RegisterByAdmin";
            public const string ChangeRole = "Users.ChangeRole";
            public const string GetPermissions = "Users.GetPermissions";
            public const string AssignPermissions = "Users.AssignPermissions";
            public const string Approve = "Users.Approve";
            public const string Reject = "Users.Reject";
            public const string ToggleBlock = "Users.ToggleBlock";
            public const string GetUsersStatistics = "Users.GetUsersStatistics";
        }

        public static class Chat
        {
            public const string GetAll = "Chat.GetAll";
            public const string GetById = "Chat.GetById";
            public const string GetMessages = "Chat.GetMessages";
            public const string HardDelete = "Chat.HardDelete";
            public const string DeleteMessageForEveryone = "Chat.DeleteMessageForEveryone";
            public const string GetChatStatistics = "Chat.GetChatStatistics";
        }

        private static readonly Lazy<List<string>> _allPermissions = new Lazy<List<string>>(() =>
        {
            var allPermissions = new List<string>();
            var modules = typeof(Permissions).GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            foreach (var module in modules)
            {
                var fields = module.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                foreach (var field in fields)
                {
                    var value = field.GetValue(null)?.ToString();
                    if (value != null) allPermissions.Add(value);
                }
            }
            return allPermissions;
        });

        public static List<string> GetAllPermissions() => _allPermissions.Value;
    }
}