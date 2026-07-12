using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.Interfaces.IServices;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DonationController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        public DonationController(IPaymentService paymentService) { 
            _paymentService = paymentService;
        }
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePaymentIntent(CreateDonationRequest request)
        {
          var response = await  _paymentService.CreatePaymentAsync(request.Amount, request.Message, CurrentUserId);
          return Ok(response);
        }
    }
}
