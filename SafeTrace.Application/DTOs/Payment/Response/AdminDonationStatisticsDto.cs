namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class AdminDonationStatisticsDto
    {
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public int SucceededCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
    }
}
