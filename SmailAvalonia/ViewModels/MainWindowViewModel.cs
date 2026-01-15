using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Core;
using Core.Models;
using Core.Services;
using SmailAvalonia.Views;
using SmailAvalonia.Services;
using CommunityToolkit.Mvvm.Input;

namespace SmailAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Window? _window;
    private UserControl? _currentPage = null;
    private Session? _currentSession = null;
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

    public MainWindowViewModel(Window? window = null)
    {
        _window = window;
        CurrentPage = new AuthenticationControl();
        
        Messenger.Subscribe(Globals.NewSessionAction, message => AssignNewSession(message.Data));
        Messenger.Subscribe(Globals.NavigateToAuthenticationAction, _ => NavigateToAuthentication());
        Messenger.Subscribe(Globals.NavigateToMessageConfigurationAction, _ => NavigateToPayloadConfiguration());
        Messenger.Subscribe(Globals.NavigateToRecepientConfigurationAction, _ => NavigateToRecepientsConfiguration());
        Messenger.Subscribe(Globals.NavigateToPayloadSummaryAction, _ => NavigateToPayloadSummary());
        Messenger.Subscribe(Globals.NavigateToExecutionAction, _ => NavigateToExecution());

        GoToSettingsCommand = new
        (
            NavigateToSettings,
            () => true
        );
    }

    public async Task InitializeDataAsync()
    {
        await WsClientService.Instance.ConnectToServerWsHub();

        //TODO: This is a temporary solution. Implement input
        Console.WriteLine("Requesting websocket key update...");
        await WsClientService.Instance.UpdateWebhookSigningKey("tZSihgTH");
        SecurityVault.Instance.SetGateWayEncryptionPhrase("pwd");
    }

    public async Task OnShutdownAsync()
    {
        // Stop services
        // Remove webhooks
        // Save state
        if(_currentSession != null) await _currentSession.PrepareShutdownAsync();
    }

    private void AssignNewSession(object? obj)
    {
        if(obj is Session session) _currentSession = session;
    }

    private void NavigateToSettings()
    {
        CurrentPage = new SettingsControl();
    }

    private void NavigateToAuthentication()
    {
        CurrentPage = new AuthenticationControl(); 
    }

    private void NavigateToRecepientsConfiguration()
    {
        if (_currentSession != null ) CurrentPage = new RecepientConfiguration(_currentSession);
    }

    private void NavigateToPayloadConfiguration()
    {
        /*if (_window != null && _oldWindowState != null)
        {
            _window.CanResize = true;
            _window.WindowState = (WindowState)_oldWindowState;
        }*/
        if (_currentSession != null ) CurrentPage = new MessageConfigurationControl(_currentSession);
    }
    
    private void NavigateToPayloadSummary()
    {
        if (_currentSession != null ) CurrentPage = new PayloadSummaryControl(_currentSession);
    }

    private void NavigateToExecution()
    {
        if (_currentSession != null ) CurrentPage = new PayloadExecutionControl(_currentSession);
    }
}
