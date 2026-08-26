namespace SafeTrace.Application.DTOs.FacebookPages.Request
{
    public class CreateFacebookPageDto
    {
        [Required]
        [MaxLength(100)]
        public string FacebookPageId { get; set; } = null!;

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
