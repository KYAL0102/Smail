using System;

namespace Core;

public static class FormatChecker
{
    public static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    }

    public static bool IsValidMobile(string number)
    {
        // Adjust regex for your mobile number format
        return System.Text.RegularExpressions.Regex.IsMatch(number, @"^\+?\d{7,15}$");
    }
}
