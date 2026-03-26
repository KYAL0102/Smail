using System.Text.Json.Serialization;

namespace Core.Models;

public class TokenPackage
{
    public string Email { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset? AccessTokenExpiration { get; set; } = null;
    public string RefreshToken { get; set; } = string.Empty;

    /*
    Email = TokenPackage?.Email,
    AccessToken = TokenPackage?.AccessToken,
    AccessTokenExpiration = TokenPackage?.AccessTokenExpiration?.ToString("o") ?? null,
    RefreshToken = TokenPackage?.RefreshToken

    TokenPackage = new();
    TokenPackage.Email = data.Email;
    TokenPackage.AccessToken = data.AccessToken ?? string.Empty;
    var success = DateTimeOffset.TryParse(data.AccessTokenExpiration, out var expiration);
    if(success) TokenPackage.AccessTokenExpiration = expiration;
    TokenPackage.RefreshToken = data.RefreshToken;      
    */
}
