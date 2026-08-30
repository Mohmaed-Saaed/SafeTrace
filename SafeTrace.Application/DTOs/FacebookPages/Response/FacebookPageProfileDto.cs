namespace SafeTrace.Application.DTOs.FacebookPages.Response
{
    public class FacebookPageProfileDto
    {
        public string PageId { get; set; } = null!;
        public string PageName { get; set; } = null!;
        public string? PageUrl { get; set; }
    }
}
