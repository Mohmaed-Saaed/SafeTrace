using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Payment.Request
{
    public class DonationUserQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
