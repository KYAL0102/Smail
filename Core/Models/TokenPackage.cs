using System.Text.Json.Serialization;

namespace Core.Models;

public class TokenPackage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("accesstoken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("accesstokenexpiration")]
    public DateTimeOffset? AccessTokenExpiration { get; set; } = null;

    [JsonPropertyName("refreshtoken")]
    public string RefreshToken { get; set; } = string.Empty;
}
