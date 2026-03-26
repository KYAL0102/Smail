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
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmailAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Window _window;
    private readonly Task? _serverTask = null;

    public Dictionary<string, PayloadExecutionControl> ExecutionHistory { get; } = [];

    public Session? _currentSession = null;
    public Session? CurrentSession
    {
        get => _currentSession;
        set
        {
            _currentSession = value;
            OnPropertyChanged();
        }
    }

    public UserControl? _currentPage = null;
    public UserControl? CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand GoToSettingsCommand { get; set; }
    public RelayCommand GoToHomeCommand { get; set; }
    public RelayCommand GoToRuntimeCommand { get; set; }

    public MainWindowViewModel(Window window, Task? serverTask = null)
    {
        _window = window;
        _serverTask = serverTask;
        
        Messenger.Subscribe(Globals.NewSessionAction, message => AssignNewSession(message.Data));
        Messenger.Subscribe(Globals.NavigateToMessageConfigurationAction, _ => NavigateToPayloadConfiguration());
        Messenger.Subscribe(Globals.NavigateToRecepientConfigurationAction, _ => NavigateToRecepientsConfiguration());
        Messenger.Subscribe(Globals.NavigateToPayloadSummaryAction, _ => NavigateToPayloadSummary());
        Messenger.Subscribe(Globals.NavigateToExecutionAction, async msg => await NavigateToExecution(msg.Data));

        GoToSettingsCommand = new
        (
            async () => await OpenSettingsAsync(),
            () => true
        );
        GoToHomeCommand = new(NavigateToRecepientsConfiguration);
        GoToRuntimeCommand = new(OpenRuntimeSummary);
    }

    public async Task InitializeDataAsync()
    {
        if(_serverTask != null) await _serverTask;
        await WsClientService.Instance.ConnectToServerWsHub();

        var dialogWindow = new Window
        {
            Title = "Create new Session",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 600, Height = 450
        };

        var control = new AuthenticationControl(dialogWindow);
        dialogWindow.Content = control;

        var sessionResult = await dialogWindow.ShowDialog<Session>(_window);
        
        if (sessionResult == null)
        {
            sessionResult = new Session(); 
        }

        CurrentSession = sessionResult;
        NavigateToRecepientsConfiguration();
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

    private void OpenRuntimeSummary()
    {
        CurrentPage = new PayloadHistoryControl(ExecutionHistory);
    }

    private void NavigateToRecepientsConfiguration()
    {
        if (CurrentSession != null) CurrentPage = new RecepientConfiguration(CurrentSession);
    }

    private void NavigateToPayloadConfiguration()
    {
        if (CurrentSession != null ) CurrentPage = new MessageConfigurationControl(CurrentSession);
    }
    
    private void NavigateToPayloadSummary()
    {
        if (CurrentSession != null ) CurrentPage = new PayloadSummaryControl(CurrentSession);
    }

    private async Task NavigateToExecution(object? obj = null)
    {
        if(obj is PayloadExecutionControl control) CurrentPage = control;
        else if (CurrentSession != null ) 
        {
            var executionControl = new PayloadExecutionControl(
                CurrentSession.Payload.Clone(), 
                CurrentSession.SmsService, 
                CurrentSession.EmailService
            );

            ExecutionHistory.Add($"Payload - {DateTime.Now.ToString("HH:mm")}", executionControl);

            CurrentPage = executionControl;

            await Task.Delay(100);
            Dispatcher.UIThread.Post(() => 
            {
                CurrentSession.Payload = new();
            }, DispatcherPriority.Background);
        }
    }
}
