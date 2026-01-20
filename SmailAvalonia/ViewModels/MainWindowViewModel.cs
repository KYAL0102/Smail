using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Core;
using Core.Models;
using Core.Services;
using SmailAvalonia.Views;
using SmailAvalonia.Services;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace SmailAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Window _window;

    private Session? _currentSession = null;
    public Session? CurrentSession 
    {
        get => _currentSession;
        set 
        {
            _currentSession = value;
            OnPropertyChanged();
        }
    }

    private UserControl? _currentPage = null;
    public UserControl? CurrentPage
    {
        get { return _currentPage; }
        set
        {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand GoToSettingsCommand { get; set; }

    public MainWindowViewModel(Window window)
    {
        _window = window;
        //CurrentPage = new AuthenticationControl();
        
        Messenger.Subscribe(Globals.NewSessionAction, message => AssignNewSession(message.Data));
        Messenger.Subscribe(Globals.NavigateToMessageConfigurationAction, _ => NavigateToPayloadConfiguration());
        Messenger.Subscribe(Globals.NavigateToRecepientConfigurationAction, _ => NavigateToRecepientsConfiguration());
        Messenger.Subscribe(Globals.NavigateToPayloadSummaryAction, _ => NavigateToPayloadSummary());
        Messenger.Subscribe(Globals.NavigateToExecutionAction, _ => NavigateToExecution());

        GoToSettingsCommand = new
        (
            async () => await OpenSettingsAsync(),
            () => true
        );
    }

    public async Task InitializeDataAsync()
    {
        await WsClientService.Instance.ConnectToServerWsHub();

        //TODO: This is a temporary solution. Implement input and permanent storage for signing key/aes passphrase
        //await WsClientService.Instance.UpdateWebhookSigningKey("tZSihgTH");
        //SecurityVault.Instance.SetWebsocketSigningKey("tZSihgTH");
        //SecurityVault.Instance.SetGateWayEncryptionPhrase("pwd");
        if(_window != null)
        {
            await Dispatcher.UIThread.InvokeAsync(async () => 
            {
                Window vm = new Window
                {
                    Title = "Create new Session",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                var control = new AuthenticationControl(vm);
                vm.Content = control;

                await vm.ShowDialog(_window);
            });
        }
    }

    public async Task OnShutdownAsync()
    {
        // Stop services
        // Remove webhooks
        // Save state
        if(CurrentSession != null) await CurrentSession.PrepareShutdownAsync();
    }

    private void AssignNewSession(object? obj)
    {
        if(obj is Session session) 
        {
            CurrentSession = session;
        }
    }

    private async Task OpenSettingsAsync()
    {
        if(_window == null || CurrentSession == null) return;

        Window vm = new Window
        {
            Title = "Settings",
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var control = new SettingsControl(CurrentSession, vm);
        vm.Content = control;

        await vm.ShowDialog(_window);
    }

    private void NavigateToRecepientsConfiguration()
    {
        if (CurrentSession != null ) CurrentPage = new RecepientConfiguration(CurrentSession);
    }

    private void NavigateToPayloadConfiguration()
    {
        /*if (_window != null && _oldWindowState != null)
        {
            _window.CanResize = true;
            _window.WindowState = (WindowState)_oldWindowState;
        }*/
        if (CurrentSession != null ) CurrentPage = new MessageConfigurationControl(CurrentSession);
    }
    
    private void NavigateToPayloadSummary()
    {
        if (CurrentSession != null ) CurrentPage = new PayloadSummaryControl(CurrentSession);
    }

    private void NavigateToExecution()
    {
        if (CurrentSession != null ) CurrentPage = new PayloadExecutionControl(CurrentSession);
    }
}
