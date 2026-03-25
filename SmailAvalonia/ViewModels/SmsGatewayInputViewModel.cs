using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using SmailAvalonia.Services;

namespace SmailAvalonia.ViewModels;

public class SmsGatewayInputViewModel : ViewModelBase
{
    private const string BTN_TEXT_EXTENDABLE = "Extend input.";
    private const string BTN_TEXT_RETRACTABLE = "Retract input.";
    private readonly SecurityVault _securityVault;
    private Session? _session = null;
    private List<Task> _tasks = [];
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

    private string _webhookSigningKey = string.Empty;
    public string WebhookSigningKey
    {
        get => _webhookSigningKey;
        set
        {
            _webhookSigningKey = value;
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

    private bool _permanentCredentialsVisible = true;
    public bool PermanentCredentialsVisible
    {
        get => _permanentCredentialsVisible;
        set
        {
            _permanentCredentialsVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _settingMode;
    public bool SettingModeEnabled 
    {
        get => _settingMode;
        set
        {
            _settingMode = value;

            PermanentCredentialsVisible = _settingMode;

            OnPropertyChanged();
        }
    }

    private string _extendBtnText = BTN_TEXT_EXTENDABLE;
    public string ExtendBtnText
    {
        get => _extendBtnText;
        set
        {
            _extendBtnText = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand ShowCredentialInput { get; set; }
    
    public SmsGatewayInputViewModel(bool settingMode, Session? session = null)
    {
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();
        SettingModeEnabled = settingMode;
        _session = session;

        ShowCredentialInput = new(ChangeLeftOverCredentialVisibility);
    }

    public async Task InitializeDataAsync()
    {
        try
        {
            await _securityVault.LoadAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
            SettingModeEnabled = true;
        }

        ResetData();
    }

    public void ChangeLeftOverCredentialVisibility()
    {
        PermanentCredentialsVisible = !PermanentCredentialsVisible;

        if(!PermanentCredentialsVisible) ExtendBtnText = BTN_TEXT_EXTENDABLE;
        else ExtendBtnText = BTN_TEXT_RETRACTABLE;
    }

    private async Task TestGateWayArguments()
    {
        try
        {
            await SmsService.TestArguments(SgIP, SgPort, SgUsername, SgPassword);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
            ErrorMessage = $"{ex.Message}";
        }
    }

    public void ResetData()
    {
        ErrorMessage = string.Empty;

        // From SecurityVault (Permanent storage)
        try
        {
            SgUsername = _securityVault.SmsGatewayUsername;
            SgPassword = _securityVault.GetGatewayPassword()?.Value ?? string.Empty;
            EncryptionPassphrase = _securityVault.GetAesPassphrase()?.Value ?? string.Empty;
            WebhookSigningKey = _securityVault.GetWhSigningKey()?.Value ?? string.Empty;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
            SettingModeEnabled = true;
        }

        if(_session == null) return;
        SgIP = _session.SmsService?.DeviceIP ?? string.Empty;
        SgPort = _session.SmsService?.Port.ToString() ?? "8080";
    }

    public async Task ConfirmParameterChangeAsync()
    {
        if(_session == null) return;

        try
        {
            //Deregister old webhooks
            await _session.SmsService.DeregisterWebhooksAsync();
            await _session.SmsService.UpdateGatewayParameters(SgIP, SgPort, SgUsername, SgPassword);
            
            _securityVault.SetWebhookSigningKey(WebhookSigningKey);
            _securityVault.SetGateWayEncryptionPhrase(EncryptionPassphrase);
            
            await _securityVault.SaveToFileAsync();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
            Dispatcher.UIThread.Post(() => 
            {
                ErrorMessage = $"{ex.Message}";
            });
        }

        //Register new webhooks
        _tasks.Add(Task.Run(_session.SmsService.RegisterWebhooks));
    }

    public async Task<SmsService?> CreateSmsServiceAsync()
    {
        SmsService? smsService = null;
        try
        {
            smsService = await SmsService.CreateNewInstance(_securityVault, SgIP, SgPort, SgUsername, SgPassword);
        }
        catch(Exception e)
        {
            Dispatcher.UIThread.Post(() => 
            {
                ErrorMessage = $"{e.Message}";
            });
            return null;
        }
        
        if(SettingModeEnabled)
        {
            _securityVault.SetGateWayCredentials(SgUsername, SgPassword);
            _securityVault.SetWebhookSigningKey(WebhookSigningKey);
            _securityVault.SetGateWayEncryptionPhrase(EncryptionPassphrase);
            _tasks.Add(Task.Run(_securityVault.SaveToFileAsync));
        }

        _tasks.Add(Task.Run(smsService.RegisterWebhooks));

        return smsService;
    }

    public async Task AwaitAllTasksAsync() => await Task.WhenAll(_tasks);
}
