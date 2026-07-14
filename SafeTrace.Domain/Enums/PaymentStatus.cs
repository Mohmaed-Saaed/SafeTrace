using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Succeeded,
        Failed,
        Cancelled,
        Refunded
    }
}
