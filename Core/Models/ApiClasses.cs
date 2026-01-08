using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Models;

public record SendMessageSchema
{
    //public string id { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumbers")]
    public string[] PhoneNumbers { get; set; } = [];
    //public int? simNumber { get; set; } = null;
    //public int? ttl { get; set; } = null;
    //public string? validUntil { get; set; } = null;
    //public bool withDeliveryReport { get; set; } = true;

    [JsonPropertyName("isEncrypted")]
    public bool IsEncrypted { get; set; } = true;
    //public int priority { get; set; } = 0;
    //public string? deviceId { get; set; } = null;

    [JsonPropertyName("textMessage")]
    public TextMessage TextMessage { get; set; } = new();
}

public class TextMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
