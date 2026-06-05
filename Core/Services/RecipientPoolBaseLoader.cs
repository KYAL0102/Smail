using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Core.Models;

namespace Core.Services;

public static class RecipientPoolBaseLoader
{
    private static string Folder => NetworkManager.GetWorkDirPath();
    public static string PoolSourcePath = "";
    public static async Task<List<Contact>?> LoadFromSourceAsync(SecurityVault securityVault)
    {
        var type = FormatChecker.GetDataSourceType(PoolSourcePath);

        if(type == DataSourceType.INVALID)
        {
            Console.WriteLine($"type is invalid or storageProvider is null.");
            return null; //TODO: POPUP that path is not valid (should not occur)
        }

        List<Contact> list = [];
        if (type == DataSourceType.LOCAL)
        {
            try
            {
                list = await GetFromFileAsync(PoolSourcePath);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
            }
        }
        else if (type == DataSourceType.URI)
        {
            try
            {
                list = await NetworkManager.FetchFromUriAsync(PoolSourcePath, securityVault.ApiKey);
                await SaveApiResultInFile(list, securityVault);
            }
            catch(Exception)
            {
                try
                {
                    var path = securityVault.StoredApiResults[PoolSourcePath];
                    list = await GetFromFileAsync(path);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
                }
            }
        }

        return list;
    }

    public static async Task<List<Contact>> GetFromFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath);

        var data = await File.ReadAllBytesAsync(filePath);
        using var stream = new MemoryStream(data);

        return await ImportController.FileContentToContactListAsync(stream, ext);
    }

    public static async Task SaveApiResultInFile(List<Contact> contacts, SecurityVault securityVault)
    {
        if(contacts.Count == 0) return;

        string fileName;
        if(securityVault.StoredApiResults.Keys.Any(k => k == PoolSourcePath)) 
            fileName = securityVault.StoredApiResults[PoolSourcePath];
        else
            fileName = $"{GenerateRandomFileName()}.csv";

        var filePath = Path.Combine(Folder, fileName);

        await SaveToCsvAsync<Contact>(contacts, filePath);
        Console.WriteLine($"Saved {contacts.Count} entries under '{filePath}'!");

        securityVault.StoredApiResults.Add(PoolSourcePath, fileName);
        await securityVault.SaveToFileAsync();
    }

    public static async Task SaveToCsvAsync<T>(List<T> data, string filePath)
    {
        if (data == null || data.Count == 0)
        {
            // Create an empty file or simply return if there is nothing to write
            await File.WriteAllTextAsync(filePath, string.Empty);
            return;
        }

        var sb = new StringBuilder();

        // 1. Get all public instance properties of the object type dynamically
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // 2. Create the Header Row (e.g., "Name,Email,MobileNumber")
        var headers = properties.Select(p => EscapeCsvValue(p.Name));
        sb.AppendLine(string.Join(",", headers));

        // 3. Create Data Rows
        foreach (var item in data)
        {
            var rowValues = properties.Select(p => 
            {
                var value = p.GetValue(item, null);
                return EscapeCsvValue(value?.ToString() ?? string.Empty);
            });
            
            sb.AppendLine(string.Join(",", rowValues));
        }

        // 4. Write out to the file using UTF-8 to preserve all special characters
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    // Helper method to handle commas, quotes, and newlines safely inside the fields
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        // If the value contains quotes, commas, or line breaks, wrap it in quotes
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            // Escape existing double quotes by doubling them up (" -> "")
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    public static string GenerateRandomFileName(int length = 12)
    {
        // Characters allowed in the filename (avoiding ambiguous symbols)
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        return string.Create(length, validChars, (buffer, chars) =>
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                // Grabs a cryptographically strong random byte
                int randomIndex = RandomNumberGenerator.GetInt32(chars.Length);
                buffer[i] = chars[randomIndex];
            }
        });
    }
}
