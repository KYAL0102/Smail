using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Core;
using Core.ApiResponseClasses;
using Core.Models;
using SmailAvalonia.Services;
using Core.Services;

namespace SmailAvalonia.ViewModels;

public class PayloadExecutionViewModel: ViewModelBase
{
    public MessagePayload Payload { get; init; }
    private Session Session { get; init; }
    public PayloadExecutionViewModel(Session session, MessagePayload payload)
    {
        Session = session;
        Payload = payload;
    }

    public ObservableCollection<ContactSendStatus> ContactStates { get; init; } = [];

    public async Task InitializeDataAsync()
    {
        RegisterToWebsocketEvent();

        var emailContacts = Payload.Contacts
            .Where(kvp => kvp.Value == TransmissionType.Email)
            .Select(kvp => kvp.Key.Email)
            .ToList();
        
        var smsContacts = Payload.Contacts
            .Where(kvp => kvp.Value == TransmissionType.SMS)
            .Select(kvp => kvp.Key.MobileNumber)
            .ToList();
        
        var message = Payload.Message;

        var smsRecipients = await Session.SmsService.SendMessageAsync(message, smsContacts);
        smsRecipients
        .Select(r => {
            Enum.TryParse<SendStatus>(r.State, true, out var status);
            var contact = Payload.Contacts.Keys.SingleOrDefault(c => c.MobileNumber == r.PhoneNumber);
            return contact is null ? null : new ContactSendStatus {
                TransmissionType = TransmissionType.SMS,
                Contact = contact,
                Status = status
            };
        })
        .Where(x => x != null)!
        .ToList()
        .ForEach(ContactStates.Add);
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
        var response = JsonSerializer.Deserialize<Webhook>(body);

        if (response != null) 
        {
            response.Payload = response?.Event switch
            {
                "sms:failed" => response.JsonPayload.Deserialize<FailedPayload>(),
                "sms:delivered" => response.JsonPayload.Deserialize<DeliveredPayload>(),
                "email:sent" => response.JsonPayload.Deserialize<SentPayload>(),
                _ => null
            };

            
        }

        Enum.TryParse<SendStatus>(response?.Event.Split(':')[1], true, out var status);
        
        var encryptor = new AesEncryptor(Globals.AesPassphrase);
        var encryptedNumber = response?.Payload?.PhoneNumber;
        var number = encryptor.Decrypt(encryptedNumber);
        
        var cs = ContactStates
            .SingleOrDefault(state => state.Contact.MobileNumber == number);
        
        //Console.WriteLine($"Setting {status} for {cs.Contact.Name}...");
        if(cs != null) cs.Status = status;
        //TODO: Extract data from payload depending on event type
    }
}
