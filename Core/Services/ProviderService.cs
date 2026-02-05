using System.Net.Mail;
using Core.Models;

namespace Core.Services;

public static class ProviderService
{
    private readonly static List<Provider> _providers = new()
    {
        new Provider
        {
            Name = "Google",
            SecretsPath = "avares://SmailAvalonia/Secrets/client_google.json",
            AuthorityUrl = "accounts.google.com",
            Scope = "openid profile email https://www.googleapis.com/auth/gmail.send",
            EmailDomains = 
            [
                "gmail.com",
                "googlemail.com"
            ]
        },
        new Provider
        {
            Name = "Microsoft",
            SecretsPath = "avares://SmailAvalonia/Secrets/client_microsoft.json",
            AuthorityUrl = "login.microsoftonline.com/common/v2.0",
            Scope = "openid profile email offline_access Mail.Send",
            EmailDomains = 
            [
                "outlook.com",
                "hotmail.com",
                "live.com",
                "hilfedieankommt.at" //TODO: Remove this in the future
            ]
        },
        new Provider
        {
            Name = "Yahoo",
            AuthorityUrl = "login.yahoo.com",
            EmailDomains = 
            [
                "yahoo.com"
            ]
        },
        new Provider
        {
            Name = "Apple",
            AuthorityUrl = "appleid.apple.com",
            EmailDomains = 
            [
                "icloud.com"
            ]
        }
    };

    public static Provider? GetServerProviderFromEmail(string email)
    {
        var emailAddress = new MailAddress(email);

        return _providers.SingleOrDefault(p => p.EmailDomains.Contains(emailAddress.Host));
    }
}
