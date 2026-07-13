using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.DTOs.Payment.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IPaymentService
    {
        public Task<ApiResponse<CreatePaymentResponseDto>> CreatePaymentPaymobAsync(decimal amount, string? message,string? CurrentUserId);
        public Task ProcessWebhookPaymobAsync(JsonElement payload, string? query);
        Task<ApiResponse<string>> GetPaymentResultAsync(IQueryCollection query);
        Task<PaginationResponseDto<DonationDto>> GetUserPaymentsAsync(string userId);
        Task<PaginationResponseDto<DonationDto>> GetPaymentsAsync(DonationQueryDto query);
        Task<ApiResponse<DonationDto>> GetPaymentByIdAsync(int id);
    }
}
