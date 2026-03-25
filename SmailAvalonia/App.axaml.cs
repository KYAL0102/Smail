using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using SmailAvalonia.ViewModels;
using SmailAvalonia.Views;
using SmailAPI;
using System.Threading;
using System.Threading.Tasks;
using System;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Core.Services;
using Core.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace SmailAvalonia;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }
    public CancellationTokenSource _cts = new();
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        var k = Program.Configuration["LocalCredentialKey"];

        services.AddSingleton<IConfiguration>(Program.Configuration);
        services.Configure<AuthSettings>(Program.Configuration);
        services.AddSingleton<SecurityVault>(_ => new SecurityVault(k));
        services.AddSingleton<EmailProviderService>();
        services.AddTransient<SmsService>();

        ServiceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Console.CancelKeyPress += (s, e) => 
            {
                e.Cancel = true; 
                Dispatcher.UIThread.Post(() => desktop.Shutdown());
            };

            var vault = ServiceProvider.GetRequiredService<SecurityVault>();

            Task.Run(async () => 
            {
                try 
                {
                    await ApiServer.RunAsync(vault, [ Program.Configuration["SmsHttpCertificateKey"] ], _cts.Token);
                }
                catch (Exception ex) 
                {
                    // On Linux, this will print to the terminal if you run ./Smail.AppImage
                    Console.WriteLine($"API CRASH: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }, _cts.Token);

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow(ApiServer.ReadyTask);
            //var control = new MainWindowViewModel(desktop.MainWindow);
            //desktop.MainWindow.DataContext = control;
            desktop.Exit += (s, e) =>
            {
                _cts.Cancel();
                Environment.Exit(0);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}