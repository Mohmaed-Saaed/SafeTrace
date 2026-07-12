using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class CreateIntentionResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = "";

        [JsonPropertyName("intention_order_id")]
        public long IntentionOrderId { get; set; }
    }
}
