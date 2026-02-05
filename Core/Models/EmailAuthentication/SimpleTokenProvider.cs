using Microsoft.Kiota.Abstractions.Authentication;

namespace Core.Models.EmailAuthentication;

public class SimpleTokenProvider : IAccessTokenProvider
{
    private readonly string _token;
    public SimpleTokenProvider(string token) => _token = token;

    public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = default, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_token);
    }

    public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();
}
