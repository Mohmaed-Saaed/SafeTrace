namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateNameDTO
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد الاسم الأول عن 100 حرف.")]
        [RegularExpression(@"^[a-zA-Z\u0600-\u06FF]+$", ErrorMessage = "الاسم الأول يجب أن يكون كلمة واحدة ويحتوي على حروف عربية أو إنجليزية فقط بدون مسافات.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب.")]
        [MaxLength(100, ErrorMessage = "يجب ألا يزيد اسم العائلة عن 100 حرف.")]
        [RegularExpression(@"^[a-zA-Z\u0600-\u06FF]+( [a-zA-Z\u0600-\u06FF]+)*$", ErrorMessage = "اسم العائلة يجب أن يحتوي على حروف فقط بمسافة واحدة بين الكلمات وبدون مسافات في البداية أو النهاية.")]
        public string LastName { get; set; } = string.Empty;



    }
}
