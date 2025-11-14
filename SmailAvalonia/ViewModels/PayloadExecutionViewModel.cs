using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Models;

namespace SmailAvalonia.ViewModels;

public class PayloadExecutionViewModel: ViewModelBase
{
    public MessagePayload Payload { get; init; }
    public PayloadExecutionViewModel(MessagePayload payload)
    {
        Payload = payload == null ? new() : payload;
    }

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

        await SmsService.Instance.SendMessageAsync(message, smsContacts);
    }
}
