using NetTopologySuite.Geometries;

namespace SafeTrace.Domain.Entities
{
    public class UrgentCase : BaseCase
    {
        public Point Location { get; set; } = default!;
        public Point Location { get; set; } = default!;
        public DateTime EndDate { get; set; }
        public DateTime LimitReachDate { get; set; }
    }
}