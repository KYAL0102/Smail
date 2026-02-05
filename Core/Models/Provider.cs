namespace Core.Models;

public class Provider
{
    public string Name { get; set; } = string.Empty;
    public string SecretsPath { get; set; } = string.Empty;
    public string AuthorityUrl { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;

    public List<string> EmailDomains = [];
}
