using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.ApiResponseClasses;

public class WebhookRegistrationResponse
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string WebhookId { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    /*{
        "event":"sms:failed",
        "id":"WfBhnpTNIY62Lp6eNeYKq",
        "source":"Local",
        "url":"https://192.168.0.191:5001/api/webhook"
    }*/
}
