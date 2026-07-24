namespace SafeTrace.Application.Helpers
{
    public static class TranslateRoleToArabicHelper
    {
        public static string TranslateRoleToArabic(string role)
        {
            return role switch
            {
                "SuperAdmin" => "مدير النظام",
                "Admin" => "مسؤول",
                "Moderator" => "مشرف",
                "VerifiedUser" => "مستخدم موثق",
                "User" => "مستخدم غير موثق",
                _ => role
            };
        }
    }
}