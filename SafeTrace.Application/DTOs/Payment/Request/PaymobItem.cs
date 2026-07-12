using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Request
{
    public class PaymobItem
    {
        public string Name { get; set; } = string.Empty;

        public long Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
