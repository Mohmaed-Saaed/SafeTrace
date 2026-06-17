using SafeTrace.Domain.Enums;

namespace SafeTrace.Domain.Entities
{
    public class UserOtp
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public OtpType Type { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}