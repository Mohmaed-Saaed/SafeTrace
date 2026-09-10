using NetTopologySuite.Geometries;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FName { get; set; } = null!;

        public string LName { get; set; } = null!;

        public string? IdentificationImageFront { get; set; }

        public string? IdentificationImageback { get; set; }

        public string? ProfileImage { get; set; }

        public VerificationStatus VerificationStatus { get; set; }

        public Point? CurrentLocation { get; set; }
        
        public Point? HomeLocation { get; set; }

        public ICollection<Case> Cases { get; set; } = new List<Case>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
        public ICollection<UserOtp> UserOtps { get; set; } = new List<UserOtp>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<AiSearchUsage> AiSearchUsages { get; set; } = new List<AiSearchUsage>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<FacebookPage> FacebookPages { get; set; } = new List<FacebookPage>();
    }
}
