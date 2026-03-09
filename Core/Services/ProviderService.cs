using System.Net.Mail;
using DnsClient;
using Core.Models;

namespace Core.Services;

public static class ProviderService
{
    private static readonly LookupClient _dnsClient = new LookupClient();
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
                //"hilfedieankommt.at"
            ]
        }/*,
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
        }*/
    };

    public static async Task<Provider?> GetServerProviderFromEmailAsync(string email)
    {
        var domain = new MailAddress(email).Host;

        // 1. Check hardcoded common domains first (Efficiency)
        var fastMatch = _providers.FirstOrDefault(p => p.EmailDomains.Contains(domain));
        if (fastMatch != null) return fastMatch;

        // 2. Perform DNS MX Lookup
        var result = await _dnsClient.QueryAsync(domain, QueryType.MX);
        var mxRecords = result.Answers.MxRecords();

        foreach (var record in mxRecords)
        {
            string exchange = record.Exchange.Value.ToLower();

            if (exchange.Contains("outlook.com"))
                return _providers.First(p => p.Name == "Microsoft");

            if (exchange.Contains("google.com") || exchange.Contains("googlemail.com"))
                return _providers.First(p => p.Name == "Google");
            
            /*if (exchange.Contains("yahoodns.net"))
                return _providers.First(p => p.Name == "Yahoo");
            
            if (exchange.Contains("icloud.com"))
                return _providers.First(p => p.Name == "Apple");*/
        }

        return null;
    }
}
