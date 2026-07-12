using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SafeTrace.Application.DTOs.Payment.Request
{
    public class CreateIntentionRequest
    {
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "EGP";

        [JsonPropertyName("payment_methods")]
        public List<int> PaymentMethods { get; set; } = [];

        [JsonPropertyName("items")]
        public List<PaymobItem> Items { get; set; } = [];

        [JsonPropertyName("billing_data")]
        public BillingData BillingData { get; set; } = new();

        [JsonPropertyName("extras")]
        public Dictionary<string, object>? Extras { get; set; }

        [JsonPropertyName("special_reference")]
        public string SpecialReference { get; set; } = "";

        [JsonPropertyName("expiration")]
        public int Expiration { get; set; } = 3600;

        [JsonPropertyName("notification_url")]
        public string NotificationUrl { get; set; } = "";

        [JsonPropertyName("redirection_url")]
        public string RedirectionUrl { get; set; } = "";
    }
}
