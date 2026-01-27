namespace Core.Models;

public class Provider
{
    public string Name { get; set; } = string.Empty;
    public string SecretsPath { get; set; } = string.Empty;
    public string AuthorityUrl { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public MailKit.Security.SecureSocketOptions Security { get; set; }

    public List<string> EmailDomains = [];
}
