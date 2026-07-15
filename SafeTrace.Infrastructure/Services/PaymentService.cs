using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.Payment.Request;
using SafeTrace.Application.DTOs.Payment.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Domain.Enums;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SafeTrace.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;
        public PaymentService(IConfiguration confg,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            HttpClient httpClient,
            ILogger<PaymentService> logger,
            IMapper mapper
            )
        {
            _config = confg;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _httpClient = httpClient;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task<ApiResponse<CreateDonationResponseDto>> CreateDonationPaymobAsync(CreateDonationRequestDto request, string? currentUserId)
        {
            if (request.Amount <= 0)
            {
                _logger.LogWarning("حاول المستخدم إنشاء تبرع بمبلغ غير صالح: {Amount}", request.Amount);
                throw new BadRequestException("المبلغ يجب أن يكون أكبر من صفر.");
            }

            ApplicationUser? user = null;
            if (!string.IsNullOrEmpty(currentUserId))
                user = await _userManager.FindByIdAsync(currentUserId);
            

            var secretKey = _config["Paymob:SecretKey"];
            var paymentMethodId = int.Parse(_config["Paymob:PaymentMethodId"]!);
            var notificationUrl = _config["Paymob:NotificationUrl"]!;
            var redirectionUrl = _config["Paymob:RedirectionUrl"]!;

            var donation = new Donation
            {
                UserId = currentUserId,
                Amount = request.Amount,
                Currency = "EGP",
                Message = request.Message,
                Reference = $"DON-{Guid.NewGuid():N}"[..20],
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Donation>().CreateAsync(donation);

            var result = await _unitOfWork.SaveAsync();

            if (result <= 0)
                throw new Exception("فشل في إنشاء التبرع.");

            var requestBody = new CreateIntentionRequestDto
            {
                Amount = (long)(request.Amount * 100),

                Currency = "EGP",

                PaymentMethods = new()
    {
        paymentMethodId
    },

                Items = new()
    {
        new PaymobItemDto
        {
            Name = "Leqaa Donation",
            Amount = (long)(request.Amount * 100),
            Description = request.Message ?? "Donation",
            Quantity = 1
        }
    },

                BillingData = new BillingData
                {
                    FirstName = user?.FName ?? "Guest",
                    LastName = user?.LName ?? "User",
                    Email = user?.Email ?? "guestLeqaa.com",
                    PhoneNumber = user?.PhoneNumber ?? "01000000000"
                },

                Extras = new()
    {
        { "Donation", true }
    },

                SpecialReference = donation.Reference,

                Expiration = 3600,

                NotificationUrl = notificationUrl,

                RedirectionUrl = redirectionUrl
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
                donation.PaymentStatus = PaymentStatus.Failed;

                await _unitOfWork.SaveAsync();

                throw new Exception(error);
            }

            var resultResponse = await response.Content.ReadFromJsonAsync<CreateIntentionResponseDto>();


            if (resultResponse == null)
                throw new Exception("استجابة غير صالحة من Paymob.");

            donation.OrderId = resultResponse.Id.ToString();

            await _unitOfWork.SaveAsync();

            var publicKey = _config["Paymob:PublicKey"];
            var clientSecret = resultResponse.ClientSecret;
            var baseUrl = _config["Paymob:BaseUrl"];

            var checkoutUrl =
                $"{baseUrl}/unifiedcheckout/?publicKey={publicKey}&clientSecret={clientSecret}";

            return ApiResponse<CreateDonationResponseDto>.Ok(new()
            {
                DonationId = donation.Id,
                CheckoutUrl = checkoutUrl
            });
        }
        public async Task ProcessWebhookPaymobAsync(JsonElement payload, string? hmac)
        {
            var obj = payload.GetProperty("obj");

            if (string.IsNullOrEmpty(hmac))
            {
                _logger.LogWarning("تم استلام Webhook بدون HMAC.");
                throw new PaymentVerificationException("HMAC مفقود.");
            }

            if (!ValidateHmacPayload(obj, hmac))
            {
                _logger.LogWarning("تم استلام Webhook مع HMAC غير صالح. المستلم: {hmac}", hmac);
                throw new PaymentVerificationException("HMAC غير صالح.");
            }

            var reference = obj.GetProperty("order").GetProperty("merchant_order_id").GetString();

            var donation = await _unitOfWork.Repository<Donation>().Query().FirstOrDefaultAsync(d => d.Reference == reference);

            if (donation == null)
                throw new NotFoundException("التبرع غير موجود.");

            if (donation.PaymentStatus == PaymentStatus.Succeeded)
            {
                _logger.LogInformation("تمت معالجة التبرع بالمعرف {reference} بنجاح بالفعل.", reference);
                return;
            }

            var amount = obj.GetProperty("amount_cents").GetInt32();
            var expectedAmount = (int)(donation.Amount * 100);

            if (amount != expectedAmount)
            {
                _logger.LogWarning("خطأ في المبلغ للتبرع بالمعرف {reference}. المتوقع: {ExpectedAmount}, المستلم: {ReceivedAmount}", reference, expectedAmount, amount);
                throw new PaymentVerificationException("خطأ في المبلغ.");
            }

            donation.TransactionId = obj.GetProperty("id").ToString();

            donation.PaymentMethod = obj.GetProperty("source_data").GetProperty("sub_type").GetString();

            var success = obj.GetProperty("success").GetBoolean();
            var pending = obj.GetProperty("pending").GetBoolean();

            if (success)
                donation.PaymentStatus = PaymentStatus.Succeeded;
            else if (pending)
                donation.PaymentStatus = PaymentStatus.Pending;
            else
                donation.PaymentStatus = PaymentStatus.Failed;

            var paymentStatus = obj.GetProperty("order").GetProperty("payment_status").GetString();

            if (!string.Equals(paymentStatus, "PAID", StringComparison.OrdinalIgnoreCase))
                donation.PaymentStatus = PaymentStatus.Failed;

            donation.PaidAt = obj.GetProperty("created_at").GetDateTime();

            await _unitOfWork.SaveAsync();
        }
        public async Task<ApiResponse<string>> GetPaymentResultAsync(IQueryCollection query)
        {
            if (query == null)
            {
                _logger.LogWarning("تم استلام مجموعة استعلام فارغة في GetPaymentResultAsync.");
                throw new ArgumentNullException(nameof(query));
            }

            if (!ValidateHmacQuery(query))
            {
                _logger.LogWarning("تم استلام رد الاتصال مع HMAC غير صالح. HMAC: {Hmac}", query["hmac"]!);
                throw new PaymentVerificationException("HMAC غير صالح.");
            }

            if (!query.TryGetValue("success", out var successValue) ||!bool.TryParse(successValue, out var isSuccess))
            {
                _logger.LogWarning("معامل success مفقود أو غير صالح.");
                throw new PaymentVerificationException("استجابة الدفع غير صالحة.");
            }

            if (!isSuccess)
            {
                _logger.LogInformation(
                    "فشل الدفع. معرف المعاملة: {TransactionId}, معرف الطلب: {OrderId}",
                    query["id"],
                    query["merchant_order_id"]);

                return ApiResponse<string>.Fail("فشل الدفع أو تم إلغاؤه.");
            }

            return ApiResponse<string>.Ok("شكراً لتبرعك.");
        }
        private bool ValidateHmacQuery(IQueryCollection query)
        {
            ArgumentNullException.ThrowIfNull(query);

            string[] fields =
            {
        "amount_cents",
        "created_at",
        "currency",
        "error_occured",
        "has_parent_transaction",
        "id",
        "integration_id",
        "is_3d_secure",
        "is_auth",
        "is_capture",
        "is_refunded",
        "is_standalone_payment",
        "is_voided",
        "order",
        "owner",
        "pending",
        "source_data.pan",
        "source_data.sub_type",
        "source_data.type",
        "success"
    };

            var sb = new StringBuilder();

            foreach (var field in fields)
            {
                if (!query.TryGetValue(field, out var value))
                    throw new PaymentVerificationException($"Missing required field '{field}'.");

                sb.Append(value);
            }

            return ValidateHmac(sb.ToString(), query["hmac"]!);
        }
        private bool ValidateHmacPayload(JsonElement obj, string receivedHmac)
        {
            var sb = new StringBuilder();

            sb.Append(obj.GetProperty("amount_cents").GetInt32());
            sb.Append(obj.GetProperty("created_at").GetString());
            sb.Append(obj.GetProperty("currency").GetString());
            sb.Append(obj.GetProperty("error_occured").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("has_parent_transaction").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("id").GetInt64());
            sb.Append(obj.GetProperty("integration_id").GetInt32());
            sb.Append(obj.GetProperty("is_3d_secure").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("is_auth").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("is_capture").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("is_refunded").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("is_standalone_payment").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("is_voided").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("order").GetProperty("id").GetInt64());
            sb.Append(obj.GetProperty("owner").GetInt32());
            sb.Append(obj.GetProperty("pending").GetBoolean().ToString().ToLowerInvariant());
            sb.Append(obj.GetProperty("source_data").GetProperty("pan").GetString());
            sb.Append(obj.GetProperty("source_data").GetProperty("sub_type").GetString());
            sb.Append(obj.GetProperty("source_data").GetProperty("type").GetString());
            sb.Append(obj.GetProperty("success").GetBoolean().ToString().ToLowerInvariant());

            return ValidateHmac(sb.ToString(), receivedHmac);
        }
        private bool ValidateHmac(string concatenatedData, string receivedHmac)
        {
            if (string.IsNullOrWhiteSpace(receivedHmac))
                throw new PaymentVerificationException("HMAC مفقود.");

            var secret = _config["Paymob:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Paymob Webhook Secret غير مهيأ.");

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));

            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenatedData));

            var calculatedHmac = Convert.ToHexString(hash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(calculatedHmac),
                Encoding.UTF8.GetBytes(receivedHmac.ToLowerInvariant()));
        }
        public async Task<PaginationResponseDto<DonationUserListDto>> GetUserDonationsAsync(string? userId, DonationUserQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new BadRequestException("معرف المستخدم مطلوب.");

            var donationsQuery = _unitOfWork.Repository<Donation>().Query(tracked: false).Where(d => d.UserId == userId && d.PaymentStatus == PaymentStatus.Succeeded);

            var totalCount = donationsQuery.Count();

            var donations = await donationsQuery.OrderByDescending(f => f.PaidAt)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

            var items = _mapper.Map<List<DonationUserListDto>>(donations);

            var response = new PaginationResponseDto<DonationUserListDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.Page,
                PageSize = query.PageSize
            };
            return response;

        }
        public async Task<PaginationResponseDto<DonationAdminListDto>> GetDonationsAsync(DonationAdminQueryDto query)
        {

            var donationsQuery = _unitOfWork.Repository<Donation>()
                .Query(tracked:false,includes: d => d.User).Where(query.Status != null ? d => d.PaymentStatus == query.Status : d => true);

            var totalCount = await donationsQuery.CountAsync();

            var donations = await donationsQuery.OrderByDescending(f => f.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

            var items = _mapper.Map<List<DonationAdminListDto>>(donations);

            var response = new PaginationResponseDto<DonationAdminListDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.Page,
                PageSize = query.PageSize
            };
            return response;
        }
    }
}
