namespace SafeTrace.Application.DTOs.FacebookPages.Request
{
    public class UpdateFacebookPageDto
    {
        [Required]
        [MaxLength(200)]
        public string PageName { get; set; } = null!;

        [Url]
        [MaxLength(2048)]
        public string? PageUrl { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = null!;
    }
}
