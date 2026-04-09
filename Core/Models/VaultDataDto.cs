namespace Core.Models;

public class VaultDataDto
{
    public string? AesPassphrase { get; set; }
    public string? WhSigningKey { get; set; }
    public string? GatewayUsername { get; set; }
    public string? GatewayPassword { get; set; }
    public string? RecepientBasePath { get; set; }
    public List<TokenPackage> TokenPackages { get; set; } = [];
}
