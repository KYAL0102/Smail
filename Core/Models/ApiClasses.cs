using System;

namespace Core.Models;

public record SendMessageSchema
{
    //public string id { get; set; } = string.Empty;
    public string[] phoneNumbers { get; set; } = [];
    //public int? simNumber { get; set; } = null;
    //public int? ttl { get; set; } = null;
    //public string? validUntil { get; set; } = null;
    //public bool withDeliveryReport { get; set; } = true;
    //public bool isEncrypted { get; set; } = false;
    //public int priority { get; set; } = 0;
    //public string? deviceId { get; set; } = null;
    public TextMessage textMessage { get; set; } = new();
}

public class TextMessage
{
    public string text { get; set; } = string.Empty;
}
