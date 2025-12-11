using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.ApiResponseClasses;

public class WebhookResponse
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public JsonElement JsonPayload { get; set; } = new();

    public Payload? Payload { get; set; }

    [JsonPropertyName("webhookId")]
    public string WebhookId { get; set; } = string.Empty;
}

