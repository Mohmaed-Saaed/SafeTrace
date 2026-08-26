namespace SafeTrace.Application.DTOs.FacebookPages.Request
{
    public class CreateFacebookPageDto
    {
        [Required(ErrorMessage = "معرّف صفحة Facebook مطلوب.")]
        [MaxLength(100, ErrorMessage = "معرّف صفحة Facebook يجب ألا يتجاوز 100 حرف.")]
        public string FacebookPageId { get; set; } = null!;

        [Required(ErrorMessage = "اسم صفحة Facebook مطلوب.")]
        [MaxLength(200, ErrorMessage = "اسم صفحة Facebook يجب ألا يتجاوز 200 حرف.")]
        public string PageName { get; set; } = null!;

        [Url(ErrorMessage = "رابط صفحة Facebook غير صالح.")]
        [MaxLength(2048, ErrorMessage = "رابط صفحة Facebook يجب ألا يتجاوز 2048 حرفًا.")]
        public string? PageUrl { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني للمستخدم مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        [MaxLength(256, ErrorMessage = "البريد الإلكتروني يجب ألا يتجاوز 256 حرفًا.")]
        public string UserEmail { get; set; } = null!;
    }
}