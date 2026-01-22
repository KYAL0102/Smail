using System.Net.Sockets;
using Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Core.Services;

public class EmailService
{
    private SmtpServerProfile? _serverProfile = null;

    public static async Task<(bool Success, string Message)> TestConnectionAsync(string host, int port, string email, string pwd)
    {
        using var client = new SmtpClient();
        try
        {
            // 1. Try to connect to the server
            // We use a short timeout so the user isn't waiting forever if the host is wrong
            client.Timeout = 10000; // 10 seconds
            await client.ConnectAsync(host, port, SecureSocketOptions.Auto);

            // 2. Try to authenticate
            await client.AuthenticateAsync(email, pwd);

            // 3. If we reached here, it worked!
            await client.DisconnectAsync(true);
            return (true, "Connection successful!");
        }
        catch (AuthenticationException)
        {
            return (false, "Invalid username or password. (Check if you need an App Password)");
        }
        catch (SslHandshakeException)
        {
            return (false, "SSL/TLS handshake failed. Check your port and security settings.");
        }
        catch (SocketException)
        {
            return (false, "Could not connect to the server. Check the Host name and Port.");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var usr = SecurityVault.Instance.Email;
        using var pwd = SecurityVault.Instance.GetEmailPassword();

        _serverProfile ??= SmtpProfiler.GetServerProfileFromEmail(usr);

        if (_serverProfile == null) throw new ArgumentException("Serverprofile could not be determined.");

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(usr));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        // 2. Send the message
        using var smtp = new SmtpClient();
        try
        {
            // Use SecureSocketOptions.StartTls for port 587
            await smtp.ConnectAsync(_serverProfile.Host, _serverProfile.Port, SecureSocketOptions.StartTls);
            
            await smtp.AuthenticateAsync(usr, pwd.Value);
            
            await smtp.SendAsync(email);
        }
        finally
        {
            // Disconnect gracefully
            await smtp.DisconnectAsync(true);
        }
    }
}
