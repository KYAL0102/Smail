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

namespace SmailAvalonia;

public partial class App : Application
{
    public CancellationTokenSource _cts = new();
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Console.CancelKeyPress += (s, e) => 
            {
                e.Cancel = true; // Prevent the OS from killing the process instantly
                Dispatcher.UIThread.Post(() => desktop.Shutdown());
            };

            Task.Run(() => ApiServer.RunAsync(System.Array.Empty<string>(), _cts.Token), _cts.Token);

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow(ApiServer.ReadyTask);
            var control = new MainWindowViewModel(desktop.MainWindow);
            desktop.MainWindow.DataContext = control;
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