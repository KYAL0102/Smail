using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Core;
using Core.Models;
using SmailAvalonia.Views;

namespace SmailAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Window? _window;
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

    public MainWindowViewModel(Window? window = null)
    {
        _window = window;
        CurrentPage = new AuthenticationControl();
        
        Messenger.Subscribe(Globals.NavigateToAuthenticationAction, _ => throw new NotImplementedException());
        Messenger.Subscribe(Globals.NavigateToPayloadConfigurationAction, message => NavigateToPayloadConfiguration(message.Data));
        Messenger.Subscribe(Globals.NavigateToPayloadSummaryAction, message => NavigateToPayloadSummary(message.Data));
        Messenger.Subscribe(Globals.NavigateToExecutionAction, message => NavigateToExecution(message.Data));
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
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
        if (obj is MessagePayload payload) CurrentPage = new PayloadExecutionControl(payload);
    }
}
