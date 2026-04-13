using Core.Models;
using Core.Models.EmailAuthentication;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using MimeKit;

namespace Core.Services;

public class EmailService
{
    private const int MSG_DELAY_MIN_MS = 10000;
    private const int MSG_DELAY_MAX_MS = 30000;
    private const int BATCH = 10;
    private const int BATCH_DELAY_MIN = 15;
    public TokenPackage TokenPackage { get; private set; }
    private readonly Provider _emailProvider;

    public EmailService(TokenPackage package, Provider provider)
    {
        TokenPackage = package;
        _emailProvider = provider;
    }

    private static readonly Random _random = new ();
    private static readonly SemaphoreSlim _semaphore = new (5);
    
    public async Task SendMessageToEmails(string message, string subject, List<Core.Models.Contact> contacts)
    {
        var email = TokenPackage.Email;
        var fromGoogleEmail = !string.IsNullOrEmpty(email) ? email : "me@gmail.com";

        var credential = GoogleCredential.FromAccessToken(TokenPackage.AccessToken);
        var service = new GmailService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = "Smail-Personal"
        });

        var counter = 0;

        foreach(var contact in contacts)
        {
            try
            {
                if(_emailProvider.Name == "Google")
                {
                    counter++;

                    int delay = _random.Next(MSG_DELAY_MIN_MS, MSG_DELAY_MAX_MS);
                    await Task.Delay(delay);

                    var name = TokenPackage.Name;
                    if(string.IsNullOrEmpty(name)) name = await GetFullNameFromGoogleAccessTokenAsync(TokenPackage.AccessToken);
                    await SendGmailAsync(service, name, fromGoogleEmail, contact, subject, message);

                    if (counter % BATCH == 0 && counter > 0) 
                    {
                        Console.WriteLine($"Batch of {BATCH} reached ({counter}). Pausing for {BATCH_DELAY_MIN} minutes...");
                        await Task.Delay(TimeSpan.FromMinutes(BATCH_DELAY_MIN));
                    }

                    Console.WriteLine("Publishing status...");
                    Messenger.Publish(new Models.Message
                    {
                        Action = Globals.EmailContactStateUpdate,
                        Data = new ContactSendStatus
                        {
                            TransmissionType = TransmissionType.Email,
                            Contact = contact,
                            Status = SendStatus.SENT
                        }
                    });
                }
                else if(_emailProvider.Name == "Microsoft") continue; //TODO
                else throw new NotSupportedException($"Provider {_emailProvider.Name} not supported");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error for {contact.Email} -> {ex.Message}");
                Messenger.Publish(new Models.Message
                {
                    Action = Globals.EmailContactStateUpdate,
                    Data = new ContactSendStatus
                    {
                        TransmissionType = TransmissionType.Email,
                        Contact = contact,
                        Status = SendStatus.FAILED,
                        Details = ex.Message
                    }
                });
            }
        }
    }

    public static async Task<string> GetFullNameFromGoogleAccessTokenAsync(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        var oauthService = new Oauth2Service(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = "Smail-Personal"
        });

        var fullName = string.Empty;
        try 
        {
            // Fetch the profile name
            var userInfo = await oauthService.Userinfo.Get().ExecuteAsync();
            fullName = userInfo.Name;
        }
        catch (Exception ex)
        {
            // Fallback if the profile scope is missing or request fails
            Console.WriteLine($"Could not retrieve profile name: {ex.Message}");
        }
        return fullName;
    }

    private static async Task SendGmailAsync(GmailService service, string fullName, string email, Models.Contact to, string subject, string body)
    {
        var mimeMessage = new MimeKit.MimeMessage();
        mimeMessage.Headers.Add("Precedence", "bulk");
        mimeMessage.Headers.Add("X-Auto-Response-Suppress", "All");
        mimeMessage.From.Add(new MailboxAddress(fullName, email)); 
        mimeMessage.To.Add(new MailboxAddress(to.Name, to.Email));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("plain") { Text = body };

        var msg = new Google.Apis.Gmail.v1.Data.Message {
            Raw = Base64UrlEncode(mimeMessage)
        };

        try 
        {
            var response = await service.Users.Messages.Send(msg, "me").ExecuteAsync();

            if (!string.IsNullOrEmpty(response.Id)) 
            {
                Console.WriteLine($"Message sent successfully! ID: {response.Id}");
            }

            var mods = new ModifyMessageRequest { 
                RemoveLabelIds = new List<string> { "INBOX", "UNREAD" } 
            };
            await service.Users.Messages.Modify(mods, "me", response.Id).ExecuteAsync();
        }
        catch (Google.GoogleApiException e) 
        {
            // This catches errors specific to the Google API (e.g., invalid tokens, rate limits)
            Console.WriteLine($"Google API Error: {e.Error.Message}");
        }
        catch (Exception e) 
        {
            // This catches general errors (e.g., no internet connection)
            Console.WriteLine($"General Error: {e.Message}");
        }
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

    private static async Task SendOutlookAsync(string accessToken, Models.Contact to, string subject, string body)
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
                    new Microsoft.Graph.Models.Recipient { EmailAddress = new EmailAddress { Name = to.Name, Address = to.Email } },
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
