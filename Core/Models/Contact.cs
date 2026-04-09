using System;

namespace Core.Models;

public class Contact
{
    public required string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HomeRegion { get; set; } = string.Empty;
    public string PayedBy { get; set; } = string.Empty;
    public string SentBy { get; set; } = string.Empty;
    public TransmissionType ContactPreference { get; set; } = TransmissionType.NONE;
}
