using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Models.EmailAuthentication;

public class ProviderSecrets
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("auth_uri")]
    public string AuthUri { get; set; } = string.Empty;

    [JsonPropertyName("redirect_uris")]
    public string[] RedirectUris { get; set; } = [];
}
