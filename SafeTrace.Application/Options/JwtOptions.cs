namespace SafeTrace.Application.Options
{
    public class JwtOptions
    {
        public string SecretKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int DurationInMinutes { get; set; }
    }
}