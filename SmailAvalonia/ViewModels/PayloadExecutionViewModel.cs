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

    public ObservableCollection<ContactSendStatus> SmsContactStates { get; init; } = [];
    public ObservableCollection<ContactSendStatus> EmailContactStates { get; init; } = [];

    public async Task InitializeDataAsync()
    {
        RegisterToWebsocketEvent();

        var emailContacts = _session.Payload.Contacts
            .Where(kvp => kvp.Value == TransmissionType.Email)
            .Select(kvp => kvp.Key)
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
            var smsRecipients = await _session.SmsService.SendMessageAsync(message, [.. recipients]);
            
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
                    if(res != null) SmsContactStates.Add(res);
                }
            });
        }
        else Console.WriteLine("Could not send SMS (SmsService was null)!");
    }

    private async Task SendEmails(string subject, string message, List<Contact> contacts)
    {
        if (_session.EmailService != null)
        {
            var tasks = _session.EmailService.SendMessageToEmails(message, subject, contacts);

            Dispatcher.UIThread.Post(() => {
                contacts
                    .Select(contact => new ContactSendStatus
                    {
                        TransmissionType = TransmissionType.Email,
                        Contact = contact,
                        Status = SendStatus.PENDING
                    })
                    .ToList()
                    .ForEach(EmailContactStates.Add);
            });

            await MonitorEmailProgress(tasks);

            Console.WriteLine("Emails sent.");
        }
        else Console.WriteLine("Could not send Email (EmailService was null.)");
    }

    public async Task MonitorEmailProgress(List<Task<ContactSendStatus>> tasks)
    {
        var remainingTasks = tasks.ToList();

        while (remainingTasks.Count != 0)
        {
            Task<ContactSendStatus> completedTask = await Task.WhenAny(remainingTasks);

            remainingTasks.Remove(completedTask);

            try
            {
                ContactSendStatus result = await completedTask;

                Dispatcher.UIThread.Post(() => {
                    var statusInList = EmailContactStates.SingleOrDefault(c => c.Contact == result.Contact);
                    if(statusInList != null) statusInList.Status = SendStatus.SENT;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

        var passphrase = SecurityVault.Instance.GetAesPassphrase().Value;
        if(!string.IsNullOrEmpty(passphrase))
        {
            var encryptor = new AesEncryptor(passphrase);
            number = encryptor.Decrypt(number);
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
