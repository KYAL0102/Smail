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
    public string Email { get; init; }

    public EmailService(string email, LoginResult result, Provider provider)
    {
        Email = email;
        _tokens = result;
        _emailProvider = provider;
    }

    private static readonly Random _random = new ();
    private static readonly SemaphoreSlim _semaphore = new (5);
    
    public List<Task<ContactSendStatus>> SendMessageToEmails(string message, string subject, List<Core.Models.Contact> contacts)
    {
        if (_tokens == null) throw new ArgumentException("No tokens.");

        var fromGoogleEmail = !string.IsNullOrEmpty(Email) ? Email : "me@gmail.com";

        return contacts.Select(contact => ExecuteSendTask(contact, () => 
        {
            return _emailProvider.Name switch
            {
                "Google" => SendGmailAsync(_tokens.AccessToken, fromGoogleEmail, contact.Email, subject, message),
                "Microsoft" => SendOutlookAsync(_tokens.AccessToken, contact.Email, subject, message),
                _ => throw new NotSupportedException($"Provider {_emailProvider.Name} not supported")
            };
        })).ToList();
    }
    
    private static async Task<ContactSendStatus> ExecuteSendTask(Models.Contact contact, Func<Task> sendAction)
    {
        await _semaphore.WaitAsync();
        try
        {
            int delay = _random.Next(500, 2000);
            await Task.Delay(delay);

            await sendAction();
            return new ContactSendStatus
            {
                TransmissionType = TransmissionType.Email,
                Contact = contact,
                Status = SendStatus.SENT
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error for {contact.Email} -> {ex.Message}");
            return new ContactSendStatus
            {
                TransmissionType = TransmissionType.Email,
                Contact = contact,
                Status = SendStatus.FAILED
            };
        }
    }

    private static async Task SendGmailAsync(string accessToken, string email, string to, string subject, string body)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        var service = new GmailService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = "Smail-Personal"
        });

        var mimeMessage = new MimeKit.MimeMessage();
        mimeMessage.From.Add(new MailboxAddress("Sender", email)); 
        mimeMessage.To.Add(new MailboxAddress("Recipient", to));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("plain") { Text = body };

        var msg = new Google.Apis.Gmail.v1.Data.Message {
            Raw = Base64UrlEncode(mimeMessage)
        };

        Console.WriteLine("Sending single email via google...");
        await service.Users.Messages.Send(msg, "me").ExecuteAsync();
    }

    private static async Task BroadcastGmailOverBccAsync(string accessToken, List<string> recipients, string subject, string body)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        var service = new GmailService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = "Smail-Personal"
        });

        var mimeMessage = new MimeKit.MimeMessage();
        mimeMessage.From.Add(new MailboxAddress("Sender", "me@gmail.com")); 
        mimeMessage.To.Add(new MailboxAddress("Recipient", recipients.First()));
        mimeMessage.Bcc.AddRange(recipients.Skip(1).Select(item => MailboxAddress.Parse(item)));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("plain") { Text = body };

        var msg = new Google.Apis.Gmail.v1.Data.Message {
            Raw = Base64UrlEncode(mimeMessage)
        };

        Console.WriteLine("Sending emails via google...");
        await service.Users.Messages.Send(msg, "me").ExecuteAsync();
    }

    private static async Task SendOutlookAsync(string accessToken, string to, string subject, string body)
    {
        var tokenProvider = new SimpleTokenProvider(accessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

        var graphClient = new GraphServiceClient(authProvider);

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
                    new Microsoft.Graph.Models.Recipient { EmailAddress = new EmailAddress { Address = to } },
                }
            },
            SaveToSentItems = true
        };

        Console.WriteLine("Sending single email via Microsoft...");
        await graphClient.Me.SendMail.PostAsync(requestBody);
    }

    private static async Task BroadcastOverOutlookBccAsync(string accessToken, List<string> recipients, string subject, string body)
    {
        var tokenProvider = new SimpleTokenProvider(accessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

        var graphClient = new GraphServiceClient(authProvider);

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
                    new Microsoft.Graph.Models.Recipient { EmailAddress = new EmailAddress { Address = recipients.First() } },
                },
                BccRecipients = recipients.Skip(1).Select(email => new Microsoft.Graph.Models.Recipient 
                {
                    EmailAddress = new EmailAddress { Address = email }
                }).ToList()
            },
            SaveToSentItems = true
        };

        Console.WriteLine("Sending emails via Microsoft...");
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
