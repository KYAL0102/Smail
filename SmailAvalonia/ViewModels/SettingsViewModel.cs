using System.Threading.Tasks;
using Core.Services;
using Core.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using SmailAvalonia.Services;
using Avalonia.Controls;

namespace SmailAvalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private Session _session;
    private Window? _window;

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

    private string _encrPassphrase = string.Empty;
    public string EncryptionPassphrase
    {
        get => _encrPassphrase;
        set 
        {
            _encrPassphrase = value;
            OnPropertyChanged();
        }
    }

    private string _whSigningKey = string.Empty;
    public string WebhookSigningKey
    {
        get => _whSigningKey;
        set
        {
            _whSigningKey = value;
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
            SaveDataCommand.NotifyCanExecuteChanged();
        }
    }

    public RelayCommand ResetDataCommand { get; set; }
    public RelayCommand SaveDataCommand { get; set; }

    public SettingsViewModel(Session session, Window? window)
    {
        _session = session;
        _window = window;

        ResetDataCommand = new(ResetData);
        SaveDataCommand = new
        (
            async () => await SaveDataAsync(),
            () => CanApply
        );

        ResetData();
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private async Task TestGateWayArguments()
    {
        try
        {
            await SmsService.TestArguments(SgIP, SgPort, SgUsername, SgPassword);
        }
        catch(Exception e)
        {
            ErrorMessage = $"{e.Message}";
        }
    }

    private void ResetData()
    {
        SgIP = _session.SmsService.DeviceIP;
        SgPort = $"{_session.SmsService.Port}";
        SgUsername = SecurityVault.Instance.GetUsername();
        SgPassword = SecurityVault.Instance.GetGatewayPassword().Value ?? string.Empty;
        EncryptionPassphrase = SecurityVault.Instance.GetAesPassphrase().Value ?? string.Empty;
        WebhookSigningKey = SecurityVault.Instance.GetWhSigningKey().Value ?? string.Empty;
    }

    private async Task SaveDataAsync()
    {
        try
        {
            CanApply = false;
            await _session.SmsService.UpdateGatewayParameters(SgIP, SgPort, SgUsername, SgPassword);
            await WsClientService.Instance.UpdateWebhookSigningKey(WebhookSigningKey);
            SecurityVault.Instance.SetWebsocketSigningKey(WebhookSigningKey);
            SecurityVault.Instance.SetGateWayEncryptionPhrase(EncryptionPassphrase);
        }
        catch(Exception e)
        {
            ErrorMessage = $"{e.Message}";
        }
        finally
        {
            CanApply = true;
        }
        
    }
}
