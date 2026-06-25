using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations.Schema;
//using System.Drawing;

namespace SafeTrace.Domain.Entities
{
    public class UrgentCase : Case
    {
        public Point Location { get; set; } = default!;
        public DateTime EndDate { get; set; }
        public DateTime LimitReachDate { get; set; }
    }
}