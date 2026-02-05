using System.IdentityModel.Tokens.Jwt;
using Core.Models;
using Core.Models.ApiResponseClasses;
using Core.Models.EmailAuthentication;
using DocumentFormat.OpenXml;
using Duende.IdentityModel.OidcClient;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using MimeKit;

namespace Core.Services;

public class EmailService
{
    private LoginResult _tokens;
    private Provider _emailProvider;

    private string? _email = null;

    public EmailService(LoginResult result, Provider provider)
    {
        _tokens = result;
        _emailProvider = provider;
    }
    
    public async Task SendMessageToEmailsAsync(string message, string subject, List<string> emails)
    {
        if (_tokens == null) throw new ArgumentException("No tokens.");

        switch(_emailProvider.Name)
        {
            case "Google":
                await SendGmailToReceivers(_tokens.AccessToken, emails, subject, message);
                break;
            case "Microsoft":
                await SendviaOutlookToRecipients(_tokens.AccessToken, emails, subject, message);
                break;
        }
    }

    private static async Task SendGmailToReceivers(string accessToken, List<string> recipients, string subject, string body)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        var service = new GmailService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = "Smail-Personal"
        });

        var mimeMessage = new MimeKit.MimeMessage();
        mimeMessage.From.Add(new MailboxAddress("Sender", "me@gmail.com")); 
        mimeMessage.To.Add(new MailboxAddress("Recipient", "me@gmail.com"));
        mimeMessage.Bcc.AddRange(recipients.Select(item => MailboxAddress.Parse(item)));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("plain") { Text = body };

        var msg = new Google.Apis.Gmail.v1.Data.Message {
            Raw = Base64UrlEncode(mimeMessage)
        };

        Console.WriteLine("Sending email via google...");
        await service.Users.Messages.Send(msg, "me").ExecuteAsync();
    }

    private static async Task SendviaOutlookToRecipients(string accessToken, List<string> recipients, string subject, string body)
    {
        // 1. Create the Auth Provider using our helper above
        var tokenProvider = new SimpleTokenProvider(accessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

        // 2. Initialize the Graph Client
        var graphClient = new GraphServiceClient(authProvider);

        // 3. Create the request body (Notice the specific v5 Body Class)
        var requestBody = new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
        {
            Message = new Microsoft.Graph.Models.Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = body,
                },
                ToRecipients = new List<Microsoft.Graph.Models.Recipient>
                {
                    new Microsoft.Graph.Models.Recipient { EmailAddress = new EmailAddress { Address = "me" } },
                },
                BccRecipients = recipients.Select(email => new Microsoft.Graph.Models.Recipient 
                {
                    EmailAddress = new EmailAddress { Address = email }
                }).ToList()
            },
            SaveToSentItems = true
        };

        // 4. Send the mail (The new v5 syntax)
        await graphClient.Me.SendMail.PostAsync(requestBody);
    }

    private static string Base64UrlEncode(MimeMessage mimeMessage)
    {
        using (var ms = new MemoryStream())
        {
            // 1. Write the MimeMessage to a stream
            mimeMessage.WriteTo(ms);
            
            // 2. Convert bytes to standard Base64
            string base64 = Convert.ToBase64String(ms.ToArray());
            
            // 3. Convert standard Base64 to Base64Url (Google's format)
            return base64
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
