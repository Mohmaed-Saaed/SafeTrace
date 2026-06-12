namespace SafeTrace.Domain.Entities
{
    public class UserSession
    {

        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public string JwtId { get; set; } = null!;

        public bool IsRevoked { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }
}