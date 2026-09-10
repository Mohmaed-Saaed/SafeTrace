namespace SafeTrace.Infrastructure.Options
{
    public sealed class FacebookGraphOptions
    {
        public const string SectionName = "FacebookGraph";

        public string BaseUrl { get; set; } = "https://graph.facebook.com/";
        public string ApiVersion { get; set; } = "v26.0";
        public int PageSize { get; set; } = 25;
        public int InitialSyncPostLimit { get; set; } = 50;
    }
}
