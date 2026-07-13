using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class DonationUserListDto
    {
        public decimal Amount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? Message { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
