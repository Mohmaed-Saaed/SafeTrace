namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateNameDTO
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد الاسم الأول عن 100 حرف.")]
        [RegularExpression(@"^[\u0600-\u06FF]+(\s+)?$", ErrorMessage = "الاسم الأول يجب أن يكون كلمة واحدة باللغة العربية فقط ولا يحتوي على مسافات أو أرقام.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد اسم العائلة عن 100 حرف.")]
        [RegularExpression(@"^[\u0600-\u06FF]+( [\u0600-\u06FF]+)*(\s+)?$", ErrorMessage = "اسم العائلة يجب أن يحتوي على حروف عربية فقط بمسافة واحدة بين الكلمات وبدون مسافات في البداية.")]
        public string LastName { get; set; } = string.Empty;



    }
}
