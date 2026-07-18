namespace SafeTrace.Application.DTOs.User.Response
{
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BannedUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int PendingVerificationUsers { get; set; }
        public int UnverifiedUsers { get; set; }
    }
}
