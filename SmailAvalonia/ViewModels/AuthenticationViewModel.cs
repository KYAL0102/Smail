using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Avalonia.Controls;
using SmailAvalonia.Views;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SmailAvalonia.ViewModels;

public class AuthenticationViewModel : ViewModelBase
{
    private const string HELP_URL = "https://github.com/KYAL0102/Smail#usage";
    private readonly Window? _window = null;

    private UserControl _currentControl = new SmsGatewayInput(false, false);
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
    public RelayCommand SkipCommand { get; init; }
    public RelayCommand NavigateToHelpCommand {get; init; }

    public AuthenticationViewModel(Window? window = null)
    {
        _window = window;
        ApplyDataCommand = new(
            async() => await ApplyDataAsync(),
            () => CanApply
        );

        NavigateToHelpCommand = new(OpenHelpSite);
        ResetCommand = new
        (
            ResetInput
        );
        SkipCommand = new
        (
            Skip
        );

        Messenger.Subscribe(Globals.ManualInputRequired, _ => { CanApply = true; });
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

        _window?.Close(_sessionInMaking);
    }

    private void ResetInput()
    {
        CanApply = true;
        if (CurrentControl is SmsGatewayInput smsInput)
        {
            smsInput.ResetData();
        }
        else if(CurrentControl is EmailInput emailInput)
        {
            emailInput.Reset();
            loginTask = null;
        }
    }

    private void Skip()
    {
        if(CurrentControl is SmsGatewayInput) CurrentControl = new EmailInput(false);
        else if(CurrentControl is EmailInput) _window?.Close(_sessionInMaking);
    }

    private async Task ApplySmsGatewayInput(SmsGatewayInput smsInput)
    {
        SmsService? smsService = await smsInput.CreateSmsServiceAsync();

        if (smsService != null) 
        {
            _sessionInMaking.SmsService = smsService;

            await smsInput.AwaitAllTasksAsync();
            CurrentControl = new EmailInput(false);
        }

        CanApply = true;
    }

    private async Task<bool> ApplyEmailInput(EmailInput emailInput)
    {
        EmailService? serviceInMaking;
        try 
        {
            if (loginTask == null || loginTask.IsCompleted)
            {
                loginTask = emailInput.ConfirmLoginAsync();
                serviceInMaking = await loginTask;
            }
            else
            {
                // The user clicked again to confirm manual input
                emailInput.ConfirmManual(); 
                serviceInMaking = await loginTask;
                Console.WriteLine($"task was faulted? {loginTask.IsFaulted}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
            ResetInput();
            return false;
        }

        _sessionInMaking.EmailService = serviceInMaking;

        return true;
    }

    public void OpenHelpSite()
    {
        if (string.IsNullOrWhiteSpace(HELP_URL) || !HELP_URL.StartsWith("http"))
        {
            throw new ArgumentException("Invalid URL. Must start with http or https.");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = HELP_URL,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var escapedUrl = HELP_URL.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {escapedUrl}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", HELP_URL);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", HELP_URL);
            }
            else
            {
                Debug.WriteLine($"Failed to open URL: {ex.Message}");
                throw;
            }
        }
    }
}
