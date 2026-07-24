using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Exceptions
{
    public class PaymentVerificationException : Exception
    {
        public PaymentVerificationException(string message) : base(message)
        {
        }
    }
}
