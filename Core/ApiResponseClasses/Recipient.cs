using System.Text.Json.Serialization;

namespace Core.ApiResponseClasses;

public class Recipient
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}
