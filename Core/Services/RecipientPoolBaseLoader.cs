using Core.Models;

namespace Core.Services;

public static class RecipientPoolBaseLoader
{
    public static string PoolSourcePath = "";
    public static async Task<List<Contact>?> LoadFromSourceAsync(string httpsThumbprint)
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
            list = await NetworkManager.FetchFromUriAsync(PoolSourcePath, httpsThumbprint);
        }

        return list;
    }

    public static async Task<List<Contact>> GetFromFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath);
    
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        return await ImportController.FileContentToContactListAsync(stream, ext);
    }
}
