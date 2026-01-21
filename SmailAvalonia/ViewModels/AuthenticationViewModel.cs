using System;
using System.Threading.Tasks;
using System.Net.Http;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Avalonia.Controls;
using SmailAvalonia.Views;

namespace SmailAvalonia.ViewModels;

public class AuthenticationViewModel : ViewModelBase
{
    private Window? _window = null;

    public SmsGatewayInput SmsInput { get; } = new();

    private bool _canApply = true;
    public bool CanApply 
    {
        get => _canApply;
        set
        {
            _canApply = value;
            OnPropertyChanged();
            ApplyDataCommand.NotifyCanExecuteChanged();
        }
    }

    public RelayCommand ApplyDataCommand { get; init; }
    public AuthenticationViewModel(Window? window = null)
    {
        _window = window;
        ApplyDataCommand = new(
            async() => await ApplyDataAsync(),
            () => CanApply
        );
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private async Task ApplyDataAsync()
    {
        CanApply = false;
        SmsService? smsService = await SmsInput.CreateSmsServiceAsync();
        CanApply = true;
        
        if (smsService == null) return;

        Messenger.Publish(new Message
        {
            Action = Globals.NewSessionAction,
            Data = new Session
            {
                SmsService = smsService
            }
        });

        await SmsInput.AwaitAllTasksAsync();

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToRecepientConfigurationAction
        });

        _window?.Close();
    }
}
