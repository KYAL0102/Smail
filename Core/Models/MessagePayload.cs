using System;
using Core.Services;

namespace Core.Models;

public class MessagePayload
{
    public string Identifier { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<Contact, TransmissionType> ContactPool { get; set; } = [];
    public TransmissionType PrimaryTransmissionType { get; set; } = TransmissionType.NONE;
    public TransmissionStrategyKey StrategyKey { get; set; } = TransmissionStrategyKey.NONE;

    public MessagePayload Clone()
    {
        return new MessagePayload
        {
            Identifier = this.Identifier,
            Subject = this.Subject,
            Message = this.Message,
            PrimaryTransmissionType = this.PrimaryTransmissionType,
            ContactPool = this.ContactPool.ToDictionary(
                kvp => new Contact { Name = kvp.Key.Name, MobileNumber = kvp.Key.MobileNumber, Email = kvp.Key.Email }, 
                kvp => kvp.Value
        )
        };
    }
}
