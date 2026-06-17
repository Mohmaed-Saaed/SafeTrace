namespace SafeTrace.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FName { get; set; } = null!;

        public string LName { get; set; } = null!;

        public string? IdentificationImage { get; set; }

        public string? ProfileImage { get; set; }

        public bool IsVerified { get; set; }

        public double? HomeLocationLatitude { get; set; }

        public double? HomeLocationLongitude { get; set; }

        public double? CurrentLocationLatitude { get; set; }

        public double? CurrentLocationLongitude { get; set; }

        public ICollection<BaseCase> Cases { get; set; } = new List<BaseCase>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
        public ICollection<UserOtp> UserOtps { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}
