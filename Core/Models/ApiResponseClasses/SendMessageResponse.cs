using System.Text.Json.Serialization;

namespace Core.Models.ApiResponseClasses;

public class SendMessageResponse
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("isEncrypted")]
    public bool IsEncrypted { get; set; }

    [JsonPropertyName("isHashed")]
    public bool IsHashed { get; set; }

    [JsonPropertyName("recipients")]
    public List<Recipient> Recipients { get; set; } = [];

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("states")]
    public Dictionary<string, string> States { get; set; } = [];
}
