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
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePaymentPaymobIntent([FromBody] CreateDonationRequestDto request)
        {
          var response = await  _paymentService.CreatePaymentPaymobAsync(request.Amount, request.Message, CurrentUserId);
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
        public async Task<IActionResult> GetPayments([FromQuery] DonationQueryDto query)
        {
            var response = await _paymentService.GetPaymentsAsync(query);

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var response = await _paymentService.GetPaymentByIdAsync(id);

            return Ok(response);
        }
        [Authorize(Roles = "User")]
        [HttpGet("get-my-payments")]
        public async Task<IActionResult> GetMyPayments()
        {
            var response = await _paymentService.GetUserPaymentsAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));

            return Ok(response);
        }
    }
}
