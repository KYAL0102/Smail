using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Core.Models;

namespace Core;

public static class ImportController
{
    private static readonly string[] RequiredHeaders = 
        { "Name", "MobileNumber", "Email", "HomeCountry", "HomeRegion", "SentBy", "PayedBy", "ContactPreference" };

    public static async Task<List<Contact>> FileContentToContactListAsync(Stream stream, string extension, bool hasHeader = true)
    {
        return extension.ToLower() switch
        {
            ".csv" => await ReadFromCsvContentAsync(stream, hasHeader),
            ".xlsx" or ".xls" => ReadFromXlsxContent(stream, hasHeader),
            _ => []
        };
    }

    public static async Task<List<Contact>> ReadFromCsvContentAsync(Stream stream, bool hasHeader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeader,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header.Replace(" ", "").ToLower()
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        var contacts = new List<Contact>();

        if (hasHeader)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
        }

        while (await csv.ReadAsync())
        {
            var contact = MapToContact(headerName => 
                hasHeader ? csv.GetField(headerName) : GetFieldByIndex(csv, headerName));

            if (contact != null) contacts.Add(contact);
        }

        return contacts;
    }

    public static List<Contact> ReadFromXlsxContent(Stream stream, bool hasHeader)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var rows = ws.RowsUsed();
        if (!rows.Any()) return [];

        var firstRow = ws.FirstRowUsed();
        var headerMap = InitializeExcelHeaderMap(hasHeader 
            ? firstRow.CellsUsed().Select(c => c.GetValue<string>()).ToArray() 
            : null);

        var dataRows = hasHeader ? rows.Skip(1) : rows;
        var contacts = new List<Contact>();

        foreach (var row in dataRows)
        {
            var contact = MapToContact(name => 
                headerMap.TryGetValue(name, out int col) ? row.Cell(col).GetValue<string>().Trim() : string.Empty);
            
            if (contact != null) contacts.Add(contact);
        }

        return contacts;
    }

    #region Private Helpers

    private static Contact? MapToContact(Func<string, string?> getValue)
    {
        var name = getValue("Name");
        if (string.IsNullOrWhiteSpace(name)) return null;

        var mobile = getValue("MobileNumber") ?? string.Empty;
        var email = getValue("Email") ?? string.Empty;
        var rawPref = getValue("ContactPreference") ?? string.Empty;

        return new Contact
        {
            Name = name,
            MobileNumber = FormatChecker.IsValidMobile(mobile) ? mobile : string.Empty,
            Email = FormatChecker.IsValidEmail(email) ? email : string.Empty,
            HomeCountry = getValue("HomeCountry") ?? string.Empty,
            HomeRegion = getValue("HomeRegion") ?? string.Empty,
            SentBy = getValue("SentBy") ?? string.Empty,
            PayedBy = getValue("PayedBy") ?? string.Empty,
            ContactPreference = Enum.TryParse<TransmissionType>(rawPref, true, out var pref) ? pref : TransmissionType.NONE
        };
    }

    private static string? GetFieldByIndex(CsvReader csv, string headerName)
    {
        // Matches the index of the header in our static RequiredHeaders array
        int index = Array.IndexOf(RequiredHeaders, headerName);
        return index >= 0 ? csv.GetField(index) : null;
    }

    private static Dictionary<string, int> InitializeExcelHeaderMap(string[]? headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (headers != null)
        {
            for (int i = 0; i < headers.Length; i++)
                map[headers[i].Replace(" ", "")] = i + 1; // Excel is 1-indexed
        }
        else
        {
            for (int i = 0; i < RequiredHeaders.Length; i++)
                map[RequiredHeaders[i]] = i + 1;
        }
        return map;
    }

    #endregion
}