using System;

namespace Core.Models;

public class MessagePayload
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<Contact, TransmissionType> Contacts { get; set; } = [];
    public TransmissionType PreferredTransmissionType { get; set; } = TransmissionType.SMS;

}
