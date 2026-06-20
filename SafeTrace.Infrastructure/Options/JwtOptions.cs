namespace SafeTrace.Infrastructure.Options
{
    public class JwtOptions
    {
        public string Secret { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public double DurationInMinutes { get; set; }
        public double RefreshTokenDurationInDays { get; set; }
    }
}