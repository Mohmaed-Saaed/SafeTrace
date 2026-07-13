using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class DonationAdminListDto
    {
        public decimal Amount { get; set; }
        public string? UserEmail { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime CreateAt { get; set; }
        public string? Message { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
