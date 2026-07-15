using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Request
{
    public class CreateDonationRequestDto
    {
        public decimal Amount { get; set; }

        public string? Message { get; set; }
    }
}
