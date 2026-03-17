using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace SmailAvalonia.Services;

public static class UpdateChecker
{
    private const string GithubSourceUrl = "https://github.com/KYAL0102/Smail";
    private static UpdateManager UpdateManager = new(new GithubSource(GithubSourceUrl, null, false));
    
    public static string GetCurrentVersion()
    {
        var locator = VelopackLocator.Current;
        
        return locator?.CurrentlyInstalledVersion?.ToString() ?? "0.0.0-development";
    }

    public static async Task<UpdateInfo?> CheckForUpdatesManualAsync()
    {
        return await UpdateManager.CheckForUpdatesAsync();
    }

    public static async Task UpdateAsync(UpdateInfo info, IProgress<int> progress)
    {
        await UpdateManager.DownloadUpdatesAsync(info, p => progress.Report(p));

        UpdateManager.ApplyUpdatesAndRestart(info);
    }
}
