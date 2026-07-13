using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class DonationDto
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
