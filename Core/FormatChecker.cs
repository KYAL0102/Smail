using System;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Validates and cleans a certificate thumbprint string.
    /// </summary>
    /// <param name="thumbprint">The raw thumbprint string from user input.</param>
    /// <param name="cleanedThumbprint">The sanitized, uppercase version of the thumbprint.</param>
    /// <returns>True if the thumbprint is a valid 40-character hex string (SHA-1).</returns>
    public static bool IsValidThumbprint(string thumbprint, out string cleanedThumbprint)
    {
        cleanedThumbprint = null;

        if (string.IsNullOrWhiteSpace(thumbprint))
            return false;

        // 1. Remove common separators (spaces, colons)
        // 2. Remove non-printable characters (like the LTR mark often copied from Windows UI)
        string cleaned = Regex.Replace(thumbprint, @"[^\da-fA-F]", "");

        // 3. Check length (SHA-1 thumbprints are 40 hex characters)
        // Note: If using SHA-256 certificates, the thumbprint length would be 64.
        if (cleaned.Length != 40)
            return false;

        // 4. Ensure it only contains hexadecimal characters
        if (!Regex.IsMatch(cleaned, @"\A\b[0-9a-fA-F]+\b\Z"))
            return false;

        cleanedThumbprint = cleaned.ToUpper();
        return true;
    }
}
