using System;

namespace Core.Models;

public class Contact
{
    public required string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
