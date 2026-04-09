using System;
using Core.Models;

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

    public static DataSourceType GetDataSourceType(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return DataSourceType.INVALID;

        string[] validExtensions = { ".csv", ".xlsx", ".xls" };

        // Check for Web URL
        if (Uri.TryCreate(input, UriKind.Absolute, out Uri? uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            return DataSourceType.URI; // We assume URLs might be APIs (no extension needed) or direct file links
        }

        // Check for Local File
        try
        {
            string extension = Path.GetExtension(input).ToLower();
            if(Path.IsPathRooted(input) && validExtensions.Contains(extension)) return DataSourceType.LOCAL;
            return DataSourceType.INVALID;
        }
        catch { return DataSourceType.INVALID; }
    }
}
