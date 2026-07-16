using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class Donation
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentGateway { get; set; } = "Paymob";
        public string? TransactionId { get; set; }
        public string? OrderId { get; set; }
        public string Reference { get; set; } = default!;
        public string? Message { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
