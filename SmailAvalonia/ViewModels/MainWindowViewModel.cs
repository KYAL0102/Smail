using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Core;
using Core.Models;
using SmailAvalonia.Views;
using SmailAvalonia.Services;

namespace SmailAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Window? _window;
    private UserControl? _currentPage = null;
    private Session? _currentSession { get; set; } = null;
    public UserControl? CurrentPage
    {
        get { return _currentPage; }
        set
        {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel(Window? window = null)
    {
        _window = window;
        CurrentPage = new AuthenticationControl();
        
        Messenger.Subscribe(Globals.NewSessionAction, message => AssignNewSession(message.Data));
        Messenger.Subscribe(Globals.NavigateToAuthenticationAction, message => NavigateToAuthentication(message.Data));
        Messenger.Subscribe(Globals.NavigateToMessageConfigurationAction, message => NavigateToPayloadConfiguration(message.Data));
        Messenger.Subscribe(Globals.NavigateToRecepientConfigurationAction, message => NavigateToRecepientsConfiguration(message.Data));
        Messenger.Subscribe(Globals.NavigateToPayloadSummaryAction, message => NavigateToPayloadSummary(message.Data));
        Messenger.Subscribe(Globals.NavigateToExecutionAction, message => NavigateToExecution(message.Data));
    }

    public async Task InitializeDataAsync()
    {
        await WsClientService.Instance.ConnectToServerWsHub();
    }

    public async Task OnShutdownAsync()
    {
        // Stop services
        // Remove webhooks
        // Save state
        await _currentSession.PrepareShutdown();
    }

    private void AssignNewSession(object? obj)
    {
        if(obj is Session session) _currentSession = session;
    }

    private void NavigateToAuthentication(object? data)
    {
        if (data is MessagePayload payload) CurrentPage = new AuthenticationControl(payload); 
    }

    private void NavigateToRecepientsConfiguration(object? obj)
    {
        if(obj is MessagePayload payload) CurrentPage = new RecepientConfiguration(payload);
    }

    private void NavigateToPayloadConfiguration(object? obj)
    {
        /*if (_window != null && _oldWindowState != null)
        {
            _window.CanResize = true;
            _window.WindowState = (WindowState)_oldWindowState;
        }*/
        if (obj is MessagePayload payload) CurrentPage = new MessageConfigurationControl(payload);
    }
    
    private void NavigateToPayloadSummary(object? obj)
    {
        if (obj is MessagePayload payload) CurrentPage = new PayloadSummaryControl(payload);
    }

    private void NavigateToExecution(object? obj)
    {
        if (_currentSession == null ) return;
        if (obj is MessagePayload payload) CurrentPage = new PayloadExecutionControl(_currentSession, payload);
    }
}
