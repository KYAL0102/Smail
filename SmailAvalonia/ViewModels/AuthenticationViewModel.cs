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
    private readonly Window? _window = null;

    private UserControl _currentControl = new SmsGatewayInput();
    public UserControl CurrentControl 
    { 
        get => _currentControl;
        private set
        {
            _currentControl = value;
            OnPropertyChanged();
        }
    }

    private Session? _sessionInMaking = null;

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

    private Task? loginTask = null;
    private async Task ApplyDataAsync()
    {
        CanApply = false;
        if (CurrentControl is SmsGatewayInput smsInput)
        {
            SmsService? smsService = await smsInput.CreateSmsServiceAsync();

            if (smsService != null) 
            {
                _sessionInMaking = new Session
                {
                    SmsService = smsService
                };

                await smsInput.AwaitAllTasksAsync();
                CurrentControl = new EmailInput();
            }

            CanApply = true;

            return;
        }
        else if(CurrentControl is EmailInput emailInput)
        {
            if (loginTask == null)
            {
                loginTask = emailInput.ConfirmLoginAsync();
                CanApply = true;
                return;
            }
            else
            {
                emailInput.ConfirmManual();
                await loginTask;
                
                if(loginTask.IsFaulted)
                {
                    CanApply = true;
                    return;
                }
            }
        }

        Messenger.Publish(new Message
        {
            Action = Globals.NewSessionAction,
            Data = _sessionInMaking
        });

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToRecepientConfigurationAction
        });

        _window?.Close();
    }
}
