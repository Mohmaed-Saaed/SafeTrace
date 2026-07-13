using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class PaymentResultDto
    {
        public long DonationId { get; set; } 

        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; }
    }
}
