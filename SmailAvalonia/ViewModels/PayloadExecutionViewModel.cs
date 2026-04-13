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
using Microsoft.Extensions.DependencyInjection;

namespace SmailAvalonia.ViewModels;

public class PayloadExecutionViewModel: ViewModelBase
{
    private readonly SecurityVault _securityVault;
    private MessagePayload _payload { get; init; }
    private SmsService? _smsService { get; init; }
    private EmailService? _emailService { get; init; }
    public PayloadExecutionViewModel(MessagePayload payload, SmsService? smsService, EmailService? emailService)
    {
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();
        _payload = payload;
        _smsService = smsService;
        _emailService = emailService;
    }

    public ObservableCollection<ContactSendStatus> SmsContactStates { get; init; } = [];
    public ObservableCollection<ContactSendStatus> EmailContactStates { get; init; } = [];

    private bool inProgress = false;

    public async Task InitializeDataAsync()
    {
        if(!inProgress)
        {
            inProgress = true;
            await ExecuteMessages();
        }
    }

    public async Task ExecuteMessages()
    {
        Messenger.Subscribe(Globals.EmailContactStateUpdate, msg => HandleEmailContactStateUpdate(msg.Data));
        RegisterToWebsocketEvent();

        var emailContacts = _payload.ContactPool
            .Where(kvp => kvp.Value == TransmissionType.Email)
            .Select(kvp => kvp.Key)
            .Distinct()
            .ToList();
        
        var smsContacts = _payload.ContactPool
            .Where(kvp => kvp.Value == TransmissionType.SMS)
            .Select(kvp => kvp.Key.MobileNumber)
            .Distinct()
            .ToList();
        
        var message = _payload.Message;
        var subject = _payload.Subject; 

        var smsTask = SendSms(subject, message, smsContacts);
        var emailTask = SendEmails(subject, message, emailContacts);

        try
        {
            await Task.WhenAll(smsTask, emailTask);
        }
        catch(Exception e)
        {
            Console.WriteLine($"{e.Message}\n{e.StackTrace}");
        }
    }

    private async Task SendSms(string subject, string message, List<string> recipients)
    {
        if (_smsService != null)
        {
            var smsRecipients = await _smsService.SendMessageAsync(subject, message, [.. recipients]);

            var results = smsRecipients
            .Select(r => {
                Enum.TryParse<SendStatus>(r.State, true, out var status);
                var contact = _payload.ContactPool.Keys.SingleOrDefault(c => c.MobileNumber == r.PhoneNumber);
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
                    if(res != null) SmsContactStates.Add(res);
                }
            });
        }
        else Console.WriteLine("Could not send SMS (SmsService was null)!");
    }

    private async Task SendEmails(string subject, string message, List<Contact> contacts)
    {
        if (_emailService != null)
        {
            contacts
                .Select(contact => new ContactSendStatus
                {
                    TransmissionType = TransmissionType.Email,
                    Contact = contact,
                    Status = SendStatus.PENDING
                })
                .ToList()
                .ForEach(EmailContactStates.Add);
            await _emailService.SendMessageToEmails(message, subject, contacts);

            Console.WriteLine("All emails sent!");
        }
        else Console.WriteLine("Could not send Emails (EmailService was null.)");
    }

    private void HandleEmailContactStateUpdate(object? obj)
    {
        Console.WriteLine("Update received!");
        if(obj is ContactSendStatus status)
        {
            Dispatcher.UIThread.Post(() => 
            {
                var item = EmailContactStates.SingleOrDefault(cs => cs.Contact == status.Contact);
                if(item != null) item.Status = status.Status;
                else Console.WriteLine($"Could not find item in list of emailContactstates!");
            });
        }
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
        
        var number = response.Payload?.PhoneNumber;

        if(!string.IsNullOrEmpty(_securityVault.GetAesPassphrase()?.Value ?? string.Empty))
        {
            var encryptor = new AesEncryptor(_securityVault.GetAesPassphrase()?.Value ?? string.Empty);
            number = encryptor.DecryptSMS(number);
        }
        
        var cs = SmsContactStates
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
