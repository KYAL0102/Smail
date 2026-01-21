using System.Threading.Tasks;
using Core.Services;
using Core.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using SmailAvalonia.Services;
using Avalonia.Controls;
using SmailAvalonia.Views;
using System.Collections.Generic;

namespace SmailAvalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private Session _session;
    private Window? _window;

    public SmsGatewayInput? SmsInput { get; } = null;

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

        SmsInput = new(_session);

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

    public async Task OnUnloadAsync()
    {
        if(SmsInput != null) await SmsInput.AwaitAllTasksAsync();
    }

    private void ResetData()
    {
        SmsInput?.ResetData();
        EncryptionPassphrase = SecurityVault.Instance.GetAesPassphrase().Value ?? string.Empty;
        WebhookSigningKey = SecurityVault.Instance.GetWhSigningKey().Value ?? string.Empty;
    }

    private async Task SaveDataAsync()
    {
        CanApply = false;

        var tasks = new List<Task>();

        if(SmsInput != null) tasks.Add(SmsInput.ConfirmParameterChangesAsync());

        tasks.Add(WsClientService.Instance.UpdateWebhookSigningKey(WebhookSigningKey));
        SecurityVault.Instance.SetWebsocketSigningKey(WebhookSigningKey);
        SecurityVault.Instance.SetGateWayEncryptionPhrase(EncryptionPassphrase);

        await Task.WhenAll(tasks);

        CanApply = true;
    }
}
