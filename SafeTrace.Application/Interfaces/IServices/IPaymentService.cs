using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IPaymentService
    {
        public Task<ApiResponse<string>> CreatePaymentAsync(decimal amount, string? message,string? CurrentUserId);
    }
}
