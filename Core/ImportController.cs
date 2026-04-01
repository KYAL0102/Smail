using System;
using System.Text.Json;
using ClosedXML.Excel;
using Core.Models;

namespace Core;

public static class ImportController
{
    public static async Task<List<Contact>> FileContentToContactListAsync(Stream stream, string extension, bool header = true)
    {
        if (extension == ".csv") return await ReadFromCsvContentAsync(stream, header);
        else if (extension == ".xlsx" || extension == ".xls") return await ReadFromXlsXContent(stream, header);

        return [];
    }

    public static async Task<List<Contact>> ReadFromCsvContentAsync(Stream stream, bool hasHeader)
    {
        using var streamReader = new StreamReader(stream);
        var fileContent = await streamReader.ReadToEndAsync();

        var lines = fileContent
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0) return [];

        // 1. Identify Header Mappings
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int dataStartIndex = 0;

        if (hasHeader)
        {
            var headers = lines[0].Split(',');
            for (int i = 0; i < headers.Length; i++)
            {
                headerMap[headers[i].Replace(" ", "").ToLower()] = i;
            }
            dataStartIndex = 1;
        }
        else
        {
            // Default mapping if no header: assume 0=Name, 1=Mobile, 2=Email
            headerMap["name"] = 0;
            headerMap["mobilenumber"] = 1;
            headerMap["email"] = 2;
            headerMap["homeRegion"] = 3;
            headerMap["contactpreference"] = 4;
        }

        // Helper to safely get value by header name
        string GetVal(string[] parts, string columnName) =>
            headerMap.TryGetValue(columnName, out int index) && index < parts.Length 
                ? parts[index].Trim() 
                : string.Empty;

        // 2. Process Data
        return lines
            .Skip(dataStartIndex)
            .Select(line => line.Split(','))
            .Select(parts => {
                var name = GetVal(parts, "name");
                var mobile = GetVal(parts, "mobilenumber");
                var email = GetVal(parts, "email");
                var homeRegion = GetVal(parts, "homeregion");
                var contactPreference = GetVal(parts, "contactpreference");

                return new Contact
                {
                    Name = name,
                    MobileNumber = FormatChecker.IsValidMobile(mobile) ? mobile : string.Empty,
                    Email = FormatChecker.IsValidEmail(email) ? email : string.Empty,
                    HomeRegion = homeRegion,
                    ContactPreference = Enum.TryParse<TransmissionType>(contactPreference, ignoreCase: true, out var pref) 
                                        ? pref 
                                        : TransmissionType.NONE
                };
            })
            .Where(c => !string.IsNullOrEmpty(c.Name)) // Ensure 'required' Name is present
            .ToList();
    }

    public static async Task<List<Contact>> ReadFromXlsXContent(Stream stream, bool hasHeader)
    {
        using var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream);
        memStream.Position = 0;

        var contacts = new List<Contact>();

        using (var workbook = new XLWorkbook(memStream))
        {
            var ws = workbook.Worksheets.First();
            var rows = ws.RowsUsed();
            if (!rows.Any()) return contacts;

            // 1. Map Headers to Column Numbers
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var firstRow = ws.FirstRowUsed();
            
            if (hasHeader)
            {
                foreach (var cell in firstRow.CellsUsed())
                {
                    headerMap[cell.GetValue<string>().Replace(" ", "")] = cell.Address.ColumnNumber;
                }
            }
            else
            {
                // Default mapping if no headers exist
                headerMap["Name"] = 1;
                headerMap["MobileNumber"] = 2;
                headerMap["Email"] = 3;
                headerMap["HomeRegion"] = 4;
                headerMap["ContactPreference"] = 5;
            }

            // Helper to safely get cell value by header name
            string GetCellValue(IXLRow row, string columnName) =>
                headerMap.TryGetValue(columnName, out int colNum) 
                    ? row.Cell(colNum).GetValue<string>().Trim() 
                    : string.Empty;

            // 2. Process Data Rows
            var dataRows = hasHeader ? rows.Skip(1) : rows;

            foreach (var row in dataRows)
            {
                var name = GetCellValue(row, "Name");
                var mobile = GetCellValue(row, "MobileNumber");
                var email = GetCellValue(row, "Email");
                var region = GetCellValue(row, "HomeRegion");
                var rawPref = GetCellValue(row, "ContactPreference");

                // Ignore rows where the required Name is missing
                if (string.IsNullOrWhiteSpace(name)) continue;

                contacts.Add(new Contact
                {
                    Name = name,
                    MobileNumber = FormatChecker.IsValidMobile(mobile) ? mobile : string.Empty,
                    Email = FormatChecker.IsValidEmail(email) ? email : string.Empty,
                    HomeRegion = region,
                    // Parse Enum safely
                    ContactPreference = Enum.TryParse<TransmissionType>(rawPref, true, out var pref) 
                                        ? pref 
                                        : TransmissionType.SMS
                });
            }
        }

        return contacts;
    }
}
