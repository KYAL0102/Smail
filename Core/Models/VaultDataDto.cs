using System.Text.Json.Serialization;

namespace Core.Models;

public class VaultDataDto
{
    [JsonPropertyName("aespassphrase")]
    public string? AesPassphrase { get; set; }

    [JsonPropertyName("whsigningkey")]
    public string? WhSigningKey { get; set; }

    [JsonPropertyName("gatewayusrname")]
    public string? GatewayUsername { get; set; }

    [JsonPropertyName("gatewaypwd")]
    public string? GatewayPassword { get; set; }

    [JsonPropertyName("httpthumbstring")]
    public string? HttpsThumbprint { get; set; }

    [JsonPropertyName("recipientbasepath")]
    public string? RecipientBasePath { get; set; }

    [JsonPropertyName("primarytype")]
    public int? PrimaryTransmissiontype { get; set; }

    [JsonPropertyName("transmissionstrategy")]
    public int? StrategyKey { get; set; }

    [JsonPropertyName("tokenpackages")]
    public List<TokenPackage> TokenPackages { get; set; } = [];
}
