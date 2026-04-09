using System.Net.Mail;
using DnsClient;
using Core.Models;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class EmailProviderService
{
    private readonly IDnsQuery _dnsClient;
    private readonly List<Provider> _providers = new()
    {
        new Provider
        {
            Name = "Google",
            AuthorityUrl = "accounts.google.com",
            Scope = "openid profile email https://www.googleapis.com/auth/gmail.modify",
            EmailDomains = 
            [
                "gmail.com",
                "googlemail.com"
            ]
        },
        new Provider
        {
            Name = "Microsoft",
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

    public EmailProviderService(IOptions<AuthSettings> options, IDnsQuery? dnsClient = null)
    {
        _dnsClient = dnsClient ?? new LookupClient();

        var settings = options.Value;
        var googleProvider = _providers.SingleOrDefault(p => p.Name == "Google");
        var microsoftProvider = _providers.SingleOrDefault(p => p.Name == "Microsoft");

        if(googleProvider == null || microsoftProvider == null)
        {
            Console.WriteLine($"Google or Microsoft item in the service list was null.");
            return;
        }
        googleProvider.Identification = settings.Google with { Name = "Google" };
        microsoftProvider.Identification = settings.Microsoft with { Name = "Microsoft"};
    }

    public async Task<Provider?> GetServerProviderFromEmailAsync(string email)
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
