using System;
using System.Threading.Tasks;
using System.Net.Http;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Avalonia.Controls;
using SmailAvalonia.Views;
using Duende.IdentityModel.OidcClient;

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

    private Session _sessionInMaking = new();

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
    public RelayCommand ResetCommand { get; init; }
    public AuthenticationViewModel(Window? window = null)
    {
        _window = window;
        ApplyDataCommand = new(
            async() => await ApplyDataAsync(),
            () => CanApply
        );
        ResetCommand = new
        (
            ResetInput
        );
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private Task<EmailService>? loginTask = null;
    private async Task ApplyDataAsync()
    {
        CanApply = false;
        if (CurrentControl is SmsGatewayInput smsInput)
        {
            await ApplySmsGatewayInput(smsInput);
            return;
        }
        else if(CurrentControl is EmailInput emailInput)
        {
            var success = await ApplyEmailInput(emailInput);
            if (!success) return;
        }

        ExitAuthentication();
    }

    private void ExitAuthentication()
    {
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

    private void ResetInput()
    {
        if (CurrentControl is SmsGatewayInput smsInput)
        {
            //TODO
        }
        else if(CurrentControl is EmailInput emailInput)
        {
            if (loginTask != null)
            {
                loginTask = null;
                emailInput.Reset();
            }
        }
    }

    private async Task ApplySmsGatewayInput(SmsGatewayInput smsInput)
    {
        SmsService? smsService = await smsInput.CreateSmsServiceAsync();

            if (smsService != null) 
            {
                _sessionInMaking.SmsService = smsService;

                await smsInput.AwaitAllTasksAsync();
                CurrentControl = new EmailInput();
            }

            CanApply = true;
    }

    private async Task<bool> ApplyEmailInput(EmailInput emailInput)
    {
        EmailService? serviceInMaking;
        if (loginTask == null || loginTask.IsCompleted)
        {
            CanApply = true;
            loginTask = emailInput.ConfirmLoginAsync();
            serviceInMaking = await loginTask;
        }
        else
        {
            try 
            {
                // The user clicked again to confirm manual input
                emailInput.ConfirmManual(); 
                serviceInMaking = await loginTask;
                Console.WriteLine($"task was faulted? {loginTask.IsFaulted}");
            }
            catch (Exception)
            {
                CanApply = true;
                loginTask = null;
                return false;
            }
        }

        _sessionInMaking.EmailService = serviceInMaking;

        return true;
    }
}
