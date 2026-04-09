using System.Text.Json.Serialization;

namespace Core.Models;

public class TokenPackage
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset? AccessTokenExpiration { get; set; } = null;
    public string RefreshToken { get; set; } = string.Empty;
}
