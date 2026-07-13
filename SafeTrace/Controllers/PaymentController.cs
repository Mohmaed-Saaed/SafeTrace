using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.Interfaces.IServices;
using System.Security.Claims;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        public PaymentController(IPaymentService paymentService) { 
            _paymentService = paymentService;
        }
        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateDonationPaymobIntent([FromBody] CreateDonationRequestDto request)
        {
          var response = await  _paymentService.CreateDonationPaymobAsync(request, CurrentUserId);
          return Ok(response);
        }

        [HttpPost("webhook")]
        public async Task WebhookPaymob([FromBody] JsonElement payload)
        {
            await _paymentService.ProcessWebhookPaymobAsync(payload, Request.Query["hmac"]);
        }

        [HttpGet("payment-result")]
        public async Task<IActionResult> GetPaymentResult()
        {
            var response = await _paymentService.GetPaymentResultAsync(Request.Query);
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetDonations([FromQuery] DonationAdminQueryDto query)
        {
            var response = await _paymentService.GetDonationsAsync(query);

            return Ok(response);
        }

        [Authorize(Roles = "User")]
        [HttpGet("get-my-payments")]
        public async Task<IActionResult> GetMyDonations([FromQuery] DonationUserQueryDto query)
        {
            var response = await _paymentService.GetUserDonationsAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), query);
            return Ok(response);
        }
    }
}
