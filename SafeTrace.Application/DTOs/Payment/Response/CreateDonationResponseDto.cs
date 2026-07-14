using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class CreateDonationResponseDto
    {
        public long DonationId { get; set; }

        public string CheckoutUrl { get; set; } = string.Empty;
    }
}
