using System;
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
        // Reads all the content of file as a text.
        var fileContent = await streamReader.ReadToEndAsync();

        List<Contact> contacts = [];

        var lines = fileContent
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        lines
            .Skip(hasHeader ? 1 : 0)
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 3)
            .Select(line => new Contact
            {
                Name = line[0],
                MobileNumber = !string.IsNullOrWhiteSpace(line[1]) && FormatChecker.IsValidMobile(line[1]) ? line[1] : string.Empty,
                Email = !string.IsNullOrWhiteSpace(line[2]) && FormatChecker.IsValidEmail(line[2]) ? line[2] : string.Empty
            })
            .ToList()
            .ForEach(contacts.Add);

        return contacts;
    }

    public static async Task<List<Contact>> ReadFromXlsXContent(Stream stream, bool hasHeader)
    {
        using var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream);
        memStream.Position = 0; // rewind before reading

        var contacts = new List<Contact>();

        using (var workbook = new XLWorkbook(memStream))
        {
            // Read the first worksheet
            var ws = workbook.Worksheets.First();

            // Skip header row if requested
            var rows = hasHeader ? ws.RowsUsed().Skip(1) : ws.RowsUsed();

            foreach (var row in rows)
            {
                var name = row.Cell(1).GetValue<string>();
                var mobile = row.Cell(2).GetValue<string>();
                var email = row.Cell(3).GetValue<string>();

                // Ignore blank rows
                if (string.IsNullOrWhiteSpace(name) &&
                    string.IsNullOrWhiteSpace(mobile) &&
                    string.IsNullOrWhiteSpace(email))
                    continue;

                contacts.Add(new Contact
                {
                    Name = name,
                    MobileNumber = mobile,
                    Email = email
                });
            }
        }

        return contacts;
    }
}
