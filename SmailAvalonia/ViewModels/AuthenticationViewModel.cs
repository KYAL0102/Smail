using System;
using System.Threading.Tasks;
using System.Net.Http;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Avalonia.Controls;

namespace SmailAvalonia.ViewModels;

public class AuthenticationViewModel : ViewModelBase
{
    private Window? _window = null;

    private string _sgIp = string.Empty;
    public string SgIP
    {
        get => _sgIp;
        set
        {
            _sgIp = value;
            OnPropertyChanged();
        }
    }

    private string _sgPort = "8080";
    public string SgPort
    {
        get => _sgPort;
        set
        {
            _sgPort = value;
            OnPropertyChanged();
        }
    }

    private string _sgUsrName = string.Empty;
    public string SgUsername
    {
        get => _sgUsrName;
        set
        {
            _sgUsrName = value;
            OnPropertyChanged();
        }
    }

    private string _sgPwd = string.Empty;
    public string SgPassword
    {
        get => _sgPwd;
        set
        {
            _sgPwd = value;
            OnPropertyChanged();
        }
    }

    private string _errorMsg = string.Empty;
    public string ErrorMessage
    {
        get => _errorMsg;
        set
        {
            _errorMsg = value;
            OnPropertyChanged();
        }
    }

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
        SmsService? smsService = null;
        try
        {
            smsService = await SmsService.CreateNewInstance(SgIP, SgPort, SgUsername, SgPassword);
        }
        catch(Exception e)
        {
            ErrorMessage = $"{e.Message}";
        }
        finally
        {
            CanApply = true;
        }

        if (smsService == null) return;

        SecurityVault.Instance.SetGateWayCredentials(SgUsername, SgPassword);

        _ = Task.Run(smsService.RegisterWebhooks);

        Messenger.Publish(new Message
        {
            Action = Globals.NewSessionAction,
            Data = new Session
            {
                SmsService = smsService
            }
        });
        SgUsername = string.Empty;
        SgPassword = string.Empty;

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToRecepientConfigurationAction
        });

        _window?.Close();
    }
}
