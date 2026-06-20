namespace SafeTrace.Application.Constants
{
    public static class Permissions
    {
        public static class UrgentCases
        {
            public const string Create = "UrgentCases.Create";
            public const string SoftDelete = "UrgentCases.SoftDelete";
            public const string HardDelete = "UrgentCases.HardDelete";
        }

        public static class LongTermCases
        {
            public const string GetAll = "LongTermCases.GetAll";
            public const string GetById = "LongTermCases.GetById";
            public const string Create = "LongTermCases.Create";
            public const string SoftDelete = "LongTermCases.SoftDelete";
            public const string HardDelete = "LongTermCases.HardDelete";
        }

        public static class UnkownCases
        {
            public const string GetAll = "UnkownCases.GetAll";
            public const string GetById = "UnkownCases.GetById";
            public const string Create = "UnkownCases.Create";
            public const string SoftDelete = "UnkownCases.SoftDelete";
            public const string HardDelete = "UnkownCases.HardDelete";
        }

        public static class FoundedCases
        {
            public const string GetAll = "FoundedCases.GetAll";
            public const string GetById = "FoundedCases.GetById";
            public const string HardDelete = "FoundedCases.HardDelete";
        }

        public static class Notifications
        {
            public const string GetAll = "Notifications.GetAll";
            public const string GetById = "Notifications.GetById";
            public const string Create = "Notifications.Create";
            public const string SoftDelete = "Notifications.SoftDelete";
            public const string HardDelete = "Notifications.HardDelete";
        }

        public static class AiMatching
        {
            public const string Search = "AiSearch.Search";
        }

        public static class Profile
        {
            public const string GetProfile = "Profile.Get";
            public const string UpdateProfile = "Profile.Update";
        }

        public static class Dashboard
        {
            public const string GetAll = "Dashboard.GetAll";
        }

        public static class Complaints
        {
            public const string Manage = "Complaints.Manage";
            public const string GetAll = "Complaints.GetAll";
            public const string GetById = "Complaints.GetById";
            public const string Create = "Complaints.Create";
            public const string HardDelete = "Complaints.HardDelete";
        }

        public static class Users
        {
            public const string Manage = "Users.Manage";
            public const string GetAll = "Users.GetAll";
            public const string ChangeRole = "Users.ChangeRole";
            public const string GetPermissions = "Users.GetPermissions";
            public const string AssignPermissions = "Users.AssignPermissions";
        }

        public static class Chat
        {
            public const string GetAll = "Chat.GetAll";
            public const string GetById = "Chat.GetById";
            public const string Create = "Chat.Create";
            public const string SoftDelete = "Chat.SoftDelete";
            public const string HardDelete = "Chat.HardDelete";
        }
    }
}