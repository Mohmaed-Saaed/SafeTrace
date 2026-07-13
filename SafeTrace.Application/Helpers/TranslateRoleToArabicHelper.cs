namespace SafeTrace.Application.Helpers
{
    public static class TranslateRoleToArabicHelper
    {
        public static string TranslateRoleToArabic(string role)
        {
            return role switch
            {
                "Admin" => "مدير النظام",
                "Moderator" => "مشرف",
                "VerifiedUser" => "مستخدم موثق",
                "User" => "مستخدم عادي",
                _ => role
            };
        }
    }
}