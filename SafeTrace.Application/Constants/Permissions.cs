namespace SafeTrace.Application.Constants
{
    public static class Permissions
    {
        public static class UrgentCases
        {
            public const string GetAll = "UrgentCases.GetAll";
            public const string GetById = "UrgentCases.GetById";
            public const string GetMyCases = "UrgentCases.GetMyCases";
            public const string Create = "UrgentCases.Create";
            public const string Update = "UrgentCases.Update";
            public const string SoftDelete = "UrgentCases.SoftDelete";
            public const string HardDelete = "UrgentCases.HardDelete";
            public const string Reject = "UrgentCases.Reject";
            public const string Approve = "UrgentCases.Approve";
            public const string MarkAsFounded = "UrgentCases.MarkAsFounded";
        }

        public static class LongTermCases
        {
            public const string GetAll = "LongTermCases.GetAll";
            public const string GetById = "LongTermCases.GetById";
            public const string GetMyCases = "LongTermCases.GetMyCases";
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
            public const string GetAll = "UnknownCases.GetAll";
            public const string GetById = "UnknownCases.GetById";
            public const string GetMyCases = "UnknownCases.GetMyCases";
            public const string Create = "UnknownCases.Create";
            public const string Update = "UnknownCases.Update";
            public const string SoftDelete = "UnknownCases.SoftDelete";
            public const string HardDelete = "UnknownCases.HardDelete";
            public const string Reject = "UnknownCases.Reject";
            public const string Approve = "UnknownCases.Approve";
            public const string MarkAsFounded = "UnknownCases.MarkAsFounded";
        }

        public static class Notifications
        {
            public const string GetMyNotifications = "Notifications.GetMyNotifications";
            public const string DeleteNotification = "Notifications.DeleteNotification";
            public const string MarkAsRead = "Notifications.MarkAsRead";
            public const string MarkAllAsRead = "Notifications.MarkAllAsRead";
        }

        public static class Profile
        {
            public const string GetUserInfo = "Profile.GetUserInfo";
            public const string UpdateName = "Profile.UpdateName";
            public const string UpdateHomeLocation = "Profile.UpdateHomeLocation";
            public const string UpdateProfileImage = "Profile.UpdateProfileImage";
            public const string UpdateIdImage = "Profile.UpdateIdImage";
            public const string UpdateUserInfo = "Profile.UpdateUserInfo";
        }

        public static class Dashboard
        {
            public const string GetStatistics = "Dashboard.GetStatistics";
        }

        public static class AiMatching
        {
            public const string Search = "AiMatching.Search";
        }

        public static class Complaints
        {
            public const string GetAll = "Complaints.GetAll";
            public const string GetById = "Complaints.GetById";
            public const string Create = "Complaints.Create";
            public const string HardDelete = "Complaints.HardDelete";
            public const string MarkAsSolved = "Complaints.MarkAsSolved";
        }

        public static class Account
        {
            public const string ChangePassword = "Account.ChangePassword";
        }

        public static class Roles
        {
            public const string GetAll = "Roles.GetAll";
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
        }

        public static class Chat
        {
            public const string GetAll = "Chat.GetAll";
            public const string GetMyChats = "Chat.GetMyChats";
            public const string GetById = "Chat.GetById";
            public const string GetMessages  = "Chat.GetMessages";
            public const string SendMessage = "Chat.SendMessage";
            public const string Create = "Chat.Create";
            public const string MarkAsRead = "Chat.MarkAsRead";
            public const string HardDelete = "Chat.HardDelete";
            public const string SoftDelete = "Chat.SoftDelete";
            public const string DeleteMessage = "Chat.DeleteMessage";
            public const string DeleteMessageForEveryone = "Chat.DeleteMessageForEveryone";
        }
    }
}