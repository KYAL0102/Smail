using System;

namespace Core.Models;

public class Contact
{
    public required string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HomeCountry { get; set; } = string.Empty;
    public string HomeRegion { get; set; } = string.Empty;
    public string PayedBy { get; set; } = string.Empty;
    public string SentBy { get; set; } = string.Empty;
    public TransmissionType ContactPreference { get; set; } = TransmissionType.NONE;

    public static Contact Dummy = new Contact
    {
        Name = "John Doe",
        MobileNumber = "+456798764",
        Email = "j.doe@gmail.com",
        HomeCountry = "United States of America",
        HomeRegion = "NYC",
        PayedBy = "Liberal Concern",
        SentBy = "Liberal Concern",
        ContactPreference = TransmissionType.Email
    };
}
