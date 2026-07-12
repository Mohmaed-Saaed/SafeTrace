using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using NetTopologySuite.Index.HPRtree;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.DTOs.Payment.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
//using System.Security.Claims;

namespace SafeTrace.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;
        public PaymentService(IConfiguration confg, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, HttpClient httpClient)
        {
            _config = confg;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _httpClient = httpClient;
        }
        public async Task<ApiResponse<string>> CreatePaymentAsync(
            decimal amount,
            string? message,
            string? currentUserId)
        {
            if (amount <= 0)
                throw new BadRequestException("Amount must be greater than zero.");

            ApplicationUser? user = null;

            if (!string.IsNullOrEmpty(currentUserId))
            {
                user = await _userManager.FindByIdAsync(currentUserId);
            }

            var secretKey = _config["Paymob:SecretKey"];
            var paymentMethodId = int.Parse(_config["Paymob:PaymentMethodId"]!);

            var donation = new Donation
            {
                UserId = currentUserId,
                Amount = amount,
                Currency = "EGP",
                Message = message,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Donation>().CreateAsync(donation);
            await _unitOfWork.SaveAsync();



            var requestBody = new CreateIntentionRequest
            {
                Amount = (long)(amount * 100),

                Currency = "EGP",

                PaymentMethods = new()
    {
        int.Parse(_config["Paymob:PaymentMethodId"]!)
    },

                Items = new()
    {
        new PaymobItem
        {
            Name = "Leqaa Donation",
            Amount = (long)(amount * 100),
            Description = message ?? "Donation",
            Quantity = 1
        }
    },

                BillingData = new BillingData
                {
                    FirstName = user?.FName ?? "Guest",
                    LastName = user?.LName ?? "User",
                    Email = user?.Email ?? "guest@safetrace.com",
                    PhoneNumber = user?.PhoneNumber ?? "01000000000"
                },

                Extras = new()
    {
        { "Donation", true }
    },

                SpecialReference = Guid.NewGuid().ToString(),

                Expiration = 3600,

                NotificationUrl = "https://your-domain/api/payment/webhook",

                RedirectionUrl = "https://localhost:4200/payment-result"
            };

            _httpClient.DefaultRequestHeaders.Clear();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", secretKey);

            var response = await _httpClient.PostAsJsonAsync(
                "https://accept.paymob.com/v1/intention/",
                requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new Exception(error);
            }

            var result = await response.Content.ReadFromJsonAsync<CreateIntentionResponse>();

            if (result == null)
                throw new Exception("Invalid response from Paymob.");

            var checkoutUrl =
                $"https://accept.paymob.com/unifiedcheckout/?publicKey={_config["Paymob:PublicKey"]}&clientSecret={result.ClientSecret}";

            return ApiResponse<string>.Ok(checkoutUrl);
        }
    }
}
