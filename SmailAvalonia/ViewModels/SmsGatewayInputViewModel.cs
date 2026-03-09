using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using Core.Models;
using Core.Services;
using SmailAvalonia.Services;

namespace SmailAvalonia.ViewModels;

public class SmsGatewayInputViewModel : ViewModelBase
{
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
            OnPropertyChanged();
            _webhookSigningKey = value;
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
    
    public SmsGatewayInputViewModel(Session? session)
    {
        _session = session;

        ResetData();
        WebhookSigningKey = SecurityVault.Instance.GetWhSigningKey().Value ?? string.Empty;
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

    public void ResetData()
    {
        if(_session == null) return;

        ErrorMessage = string.Empty;
        SgUsername = SecurityVault.Instance.GetUsername();
        SgPassword = SecurityVault.Instance.GetGatewayPassword().Value ?? string.Empty;
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
            
            await WsClientService.Instance.UpdateWebhookSigningKey(WebhookSigningKey);
            SecurityVault.Instance.SetWebsocketSigningKey(WebhookSigningKey);
        }
        catch(Exception e)
        {
            Dispatcher.UIThread.Post(() => 
            {
                ErrorMessage = $"{e.Message}";
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
            smsService = await SmsService.CreateNewInstance(SgIP, SgPort, SgUsername, SgPassword);
        }
        catch(Exception e)
        {
            Dispatcher.UIThread.Post(() => 
            {
                ErrorMessage = $"{e.Message}";
            });
            return null;
        }
        
        SecurityVault.Instance.SetGateWayCredentials(SgUsername, SgPassword);

        await WsClientService.Instance.UpdateWebhookSigningKey(WebhookSigningKey);
        SecurityVault.Instance.SetWebsocketSigningKey(WebhookSigningKey);

        _tasks.Add(Task.Run(smsService.RegisterWebhooks));

        return smsService;
    }

    public async Task AwaitAllTasksAsync() => await Task.WhenAll(_tasks);
}
