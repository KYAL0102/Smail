using System;

namespace Core.Models;

public class MessagePayload
{
    public string Identifier { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<Contact, TransmissionType> Contacts { get; set; } = [];
    public TransmissionType PreferredTransmissionType { get; set; } = TransmissionType.SMS;

    public MessagePayload Clone()
    {
        return new MessagePayload
        {
            Identifier = this.Identifier,
            Message = this.Message,
            PreferredTransmissionType = this.PreferredTransmissionType,
            Contacts = this.Contacts.ToDictionary(
                kvp => new Contact { Name = kvp.Key.Name, MobileNumber = kvp.Key.MobileNumber, Email = kvp.Key.Email }, 
                kvp => kvp.Value
        )
        };
    }
}
