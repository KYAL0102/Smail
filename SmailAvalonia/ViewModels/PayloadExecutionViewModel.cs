using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.ApiResponseClasses;
using Core.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace SmailAvalonia.ViewModels;

public class PayloadExecutionViewModel: ViewModelBase
{
    private HubConnection _connection;
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

    private void ConnectToServerWebsocket()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("http://127.0.0.1:5005/ws") // API address
            .WithAutomaticReconnect()
            .Build();

        // When server pushes update
        _connection.On<WebhookResponse>("WebhookUpdate", (data) =>
        {
            Console.WriteLine("Got update: " + data.Event);

            // Must update UI on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                HandleWebhookEvent(data);
            });
        });
    }

    private void HandleWebhookEvent(WebhookResponse response)
    {
        Console.WriteLine($"Response from webhook: {response.Event}");
    }
}
