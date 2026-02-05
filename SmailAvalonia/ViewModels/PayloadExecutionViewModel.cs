using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Core;
using Core.Models.ApiResponseClasses;
using Core.Models;
using SmailAvalonia.Services;
using Core.Services;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using Avalonia.Threading;

namespace SmailAvalonia.ViewModels;

public class PayloadExecutionViewModel: ViewModelBase
{
    private Session _session { get; init; }
    public PayloadExecutionViewModel(Session session)
    {
        _session = session;
    }

    public ObservableCollection<ContactSendStatus> ContactStates { get; init; } = [];

    public async Task InitializeDataAsync()
    {
        RegisterToWebsocketEvent();

        var emailContacts = _session.Payload.Contacts
            .Where(kvp => kvp.Value == TransmissionType.Email)
            .Select(kvp => kvp.Key.Email)
            .ToList();
        
        var smsContacts = _session.Payload.Contacts
            .Where(kvp => kvp.Value == TransmissionType.SMS)
            .Select(kvp => kvp.Key.MobileNumber)
            .ToList();
        
        var message = _session.Payload.Message;
        var emailSubject = "Test-Email"; //TODO: _session.Payload.EmailSession;

        var smsTask = SendSms(message, smsContacts);
        var emailTask = SendEmails(emailSubject, message, emailContacts);

        try
        {
            await Task.WhenAll(smsTask, emailTask);
        }
        catch(Exception e)
        {
            Console.WriteLine($"{e.Message}\n{e.StackTrace}");
        }
    }

    private async Task SendSms(string message, List<string> recipients)
    {
        if (_session.SmsService != null)
        {
            var smsRecipients = await _session.SmsService.SendMessageAsync(message, recipients);
            
            var results = smsRecipients
            .Select(r => {
                Enum.TryParse<SendStatus>(r.State, true, out var status);
                var contact = _session.Payload.Contacts.Keys.SingleOrDefault(c => c.MobileNumber == r.PhoneNumber);
                return contact is null ? null : new ContactSendStatus {
                    TransmissionType = TransmissionType.SMS,
                    Contact = contact,
                    Status = status
                };
            })
            .Where(x => x != null)!
            .ToList();

            Dispatcher.UIThread.Post(() => {
                foreach (var res in results) {
                    if(res != null) ContactStates.Add(res);
                }
            });
        }
        else Console.WriteLine("Could not send SMS (SmsService was null)!");
    }

    private async Task SendEmails(string subject, string message, List<string> recipients)
    {
        if (_session.EmailService != null)
        {
            await _session.EmailService.SendMessageToEmailsAsync(message, subject, recipients);
            Console.WriteLine("Emails sent.");
        }
        else Console.WriteLine("Could not send Email (EmailService was null.)");
    }

    private void RegisterToWebsocketEvent()
    {
        // When server pushes update
        WsClientService.Instance.On<string>("WebhookUpdate", (body) =>
        {
            // Must update UI on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                HandleWebhookEvent(body);
            });
        });
    }

    private void HandleWebhookEvent(string body)
    {
        //Console.WriteLine(body);
        var response = JsonSerializer.Deserialize<Webhook>(body);
        if(response == null) 
        {
            Console.WriteLine($"Deserialization into {nameof(Webhook)} failed! ->\n{body}");
            return;
        }

        response.Payload = response.Event switch
        {
            "sms:failed" => response.JsonPayload.Deserialize<FailedPayload>(),
            "sms:delivered" => response.JsonPayload.Deserialize<DeliveredPayload>(),
            "sms:sent" => response.JsonPayload.Deserialize<SentPayload>(),
            _ => null
        };

        Enum.TryParse<SendStatus>(response.Event.Split(':')[1], true, out var status);
        
        var encryptor = new AesEncryptor(SecurityVault.Instance.GetAesPassphrase().Value ?? string.Empty);
        var encryptedNumber = response.Payload?.PhoneNumber;
        var number = encryptor.Decrypt(encryptedNumber);
        
        var cs = ContactStates
            .SingleOrDefault(state => state.Contact.MobileNumber == number);
        
        //Console.WriteLine($"Setting {status} for {cs.Contact.Name}...");
        if(cs != null) cs.Status = status;
        if (status != SendStatus.FAILED) return;

        var payload = (FailedPayload?) response.Payload;

        if(cs != null && payload != null) 
        {
            var reason = payload.Reason.Split(':')[1].TrimStart();
            cs.Details = reason;
        }
    }
}
