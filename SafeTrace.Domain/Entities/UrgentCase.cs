using System.Drawing;

namespace SafeTrace.Domain.Entities
{
    public class UrgentCase : BaseCase
    {
        public Point? Location { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LimitReachDate { get; set; }
    }
}