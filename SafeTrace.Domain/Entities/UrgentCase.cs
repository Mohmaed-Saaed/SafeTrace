namespace SafeTrace.Domain.Entities
{
    public class UrgentCase : BaseCase
    {
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LimitReachDate { get; set; }
    }
}