using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class UrgentCase : BaseCase
    {
        public DateTime EndDate { get; set; }
        public DateTime LimitReachDate { get; set; }
    }
}
