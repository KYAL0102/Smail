namespace Core.Models;

public class SmtpServerProfile
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public MailKit.Security.SecureSocketOptions Security { get; set; }
}
