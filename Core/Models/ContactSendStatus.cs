using Core.Models;

namespace Core.ApiResponseClasses;

public class ContactSendStatus
{
    public required TransmissionType TransmissionType { get; init; }
    public required Contact Contact { get; init; }
    public required SendStatus Status { get; set; }
}
