using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace SafeTrace.API.Controllers
{
    public class PaymentController : BaseApiController
    {
        private readonly IPaymentService _paymentService;
        public PaymentController(IPaymentService paymentService) { 
            _paymentService = paymentService;
        }

        /// <summary>
        /// Create a Paymob payment intent for donation
        /// </summary>
        [HttpPost("create-donation")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateDonationPaymobIntent([FromBody] CreateDonationRequestDto request)
        {
          var response = await  _paymentService.CreateDonationPaymobAsync(request, CurrentUserIdOrNull);
          return Ok(response);
        }

        /// <summary>
        /// Paymob Webhook endpoint to update payment statuses
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task WebhookPaymob([FromBody] JsonElement payload)
        {
            await _paymentService.ProcessWebhookPaymobAsync(payload, Request.Query["hmac"]);
        }

        /// <summary>
        /// Get the result of a payment after redirect from Paymob
        /// </summary>
        [HttpGet("payment-result")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaymentResult()
        {
            var response = await _paymentService.GetPaymentResultAsync(Request.Query);
            return Ok(response);
        }

        /// <summary>
        /// Get all donations for admins
        /// </summary>
        [HttpGet("get-donations")]
        [HasPermission(Permissions.Donations.GetDonations)]
        public async Task<IActionResult> GetDonations([FromQuery] DonationAdminQueryDto query)
        {
            var response = await _paymentService.GetDonationsAsync(query);
            return Ok(response);
        }

        /// <summary>
        /// Get current user's donations
        /// </summary>
        [HttpGet("get-my-donations")]
        [Authorize]
        public async Task<IActionResult> GetMyDonations([FromQuery] DonationUserQueryDto query)
        {
            var response = await _paymentService.GetUserDonationsAsync(CurrentUserId, query);
            return Ok(response);
        }

        /// <summary>
        /// Get donation statistics for admins
        /// </summary>
        [HttpGet("admin/statistics")]
        [HasPermission(Permissions.Donations.GetDonationStatistics)]
        public async Task<IActionResult> GetDonationStatistics()
        {
            var response = await _paymentService.GetDonationStatisticsAsync();
            return Ok(response);
        }

        [HasPermission(Permissions.Donations.GenerateDonationsPdfReport)]
        [HttpPost("export-pdf")]
        public async Task<IActionResult> ExportDonationsPdf(
        [FromBody] DonationAdminQueryDto query)
        {
            var file = await _paymentService
                .GeneratePdfReportAsync(query);


            return File(
                file,
                "application/pdf",
                $"Donations_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }

    }
}
