using System.Text.Json.Serialization;

namespace Core.Models.ApiResponseClasses;

public class Payload 
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("simNumber")]
    public string? SimNumber { get; set; }
}

public class FailedPayload : Payload
{
    [JsonPropertyName("failedAt")]
    public string FailedAt { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class SentPayload : Payload
{

    [JsonPropertyName("partsCount")]
    public string PartsCount { get; set; } = string.Empty;

    [JsonPropertyName("sentAt")]
    public string SentAt { get; set; } = string.Empty;
}

public class DeliveredPayload : Payload
{
    [JsonPropertyName("deliveredAt")]
    public string DeliveredAt { get; set; } = string.Empty;
}
