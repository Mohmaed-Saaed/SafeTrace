using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class PaymobOptionsDto
    {
        public const string SectionName = "Paymob";

        public string SecretKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://accept.paymob.com";

        public int PaymentMethodId { get; set; }
    }
}
