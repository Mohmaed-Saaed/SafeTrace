using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafeTrace.Application.DTOs.Payment.Response
{
    public class PaymobWebhookResponseDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("obj")]
        public JsonElement Object { get; set; }
    }
}
