using Avalonia;
using Microsoft.Extensions.Configuration;
using System;
using Velopack;
using Velopack.Locators;

namespace SmailAvalonia;

sealed class Program
{
    public static IConfiguration Configuration { get; private set; }
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = VelopackApp.Build();
#if DEBUG
        var locator = new TestVelopackLocator(
            appId: "Smail",
            version: "0.1.0",
            packagesDir: "https://github.com/KYAL0102/Smail"
        );
        builder.SetLocator(locator);
#endif
        builder.Run();

        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<Program>(optional: true) // Reads from your local secret store
            .AddEnvironmentVariables()              // Reads from GitHub Actions/OS env vars
            .Build();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
