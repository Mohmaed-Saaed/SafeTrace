using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public Task<ApiResponse<CreateDonationResponseDto>> CreateDonationPaymobAsync(CreateDonationRequestDto request, string? CurrentUserId);
        public Task ProcessWebhookPaymobAsync(JsonElement payload, string? query);
        Task<ApiResponse<string>> GetPaymentResultAsync(IQueryCollection query);
        Task<PaginationResponseDto<DonationAdminListDto>> GetDonationsAsync(DonationAdminQueryDto query);
        Task<PaginationResponseDto<DonationUserListDto>> GetUserDonationsAsync(string? id, DonationUserQueryDto query);
    }
}
