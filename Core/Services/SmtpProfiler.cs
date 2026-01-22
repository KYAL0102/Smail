using Core.Models;
using MailKit.Security;

namespace Core.Services;

public static class SmtpProfiler
{
    private readonly static Dictionary<string, SmtpServerProfile> _smtpMapping = new()
    {
        //Google
        { "gmail.com", new SmtpServerProfile { Host = "smtp.gmail.com", Port = 587, Security = SecureSocketOptions.StartTls } },
        { "googlemail.com", new SmtpServerProfile { Host = "smtp.gmail.com", Port = 587, Security = SecureSocketOptions.StartTls } },
        
        //Microsoft
        { "outlook.com", new SmtpServerProfile { Host = "smtp.office365.com", Port = 587, Security = SecureSocketOptions.StartTls } },
        { "hotmail.com", new SmtpServerProfile { Host = "smtp.office365.com", Port = 587, Security = SecureSocketOptions.StartTls } },
        { "live.com", new SmtpServerProfile { Host = "smtp.office365.com", Port = 587, Security = SecureSocketOptions.StartTls } },
        
        //Other
        { "yahoo.com", new SmtpServerProfile { Host = "smtp.mail.yahoo.com", Port = 465, Security = SecureSocketOptions.SslOnConnect } },
        { "icloud.com", new SmtpServerProfile { Host = "smtp.mail.me.com", Port = 587, Security = SecureSocketOptions.StartTls } },
        { "me.com", new SmtpServerProfile { Host = "smtp.mail.me.com", Port = 587, Security = SecureSocketOptions.StartTls } }
    };

    public static SmtpServerProfile? GetServerProfileFromEmail(string email)
    {
        if (_smtpMapping.TryGetValue(email, out var profile)) return profile;
        return null;
    }
}
