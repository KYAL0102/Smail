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

    public EmailInput? EmailInput { get; } = null;

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

    /*private string _whSigningKey = string.Empty;
    public string WebhookSigningKey
    {
        get => _whSigningKey;
        set
        {
            _whSigningKey = value;
            OnPropertyChanged();
        }
    }*/

    private bool _canApply_smsSettings = true;
    public bool CanApply_SmsSettings 
    {
        get => _canApply_smsSettings;
        set
        {
            _canApply_smsSettings = value;
            OnPropertyChanged();
            SaveDataCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _editingEmail = false;
    public bool EditingEmail 
    {
        get => _editingEmail;
        set
        {
            _editingEmail = value;
            OnPropertyChanged();
            ApplyEmailCommand.NotifyCanExecuteChanged();
            CancelEmailEditingCommand.NotifyCanExecuteChanged();
            EditEmailCommand.NotifyCanExecuteChanged();
        }
    }

    public RelayCommand ResetDataCommand { get; set; }
    public RelayCommand SaveDataCommand { get; set; }

    public RelayCommand EditEmailCommand { get; set; }
    public RelayCommand CancelEmailEditingCommand { get; set; }
    public RelayCommand ApplyEmailCommand { get; set; }

    public SettingsViewModel(Session session, Window? window)
    {
        _session = session;
        _window = window;

        SmsInput = new(_session);
        EmailInput = new(_session);

        ResetDataCommand = new(ResetData);
        SaveDataCommand = new
        (
            async () => await SaveDataAsync(),
            () => CanApply_SmsSettings
        );
        EditEmailCommand = new
        (
            EnableEmailEditing,
            () => !EditingEmail
        );
        CancelEmailEditingCommand = new
        (
            ResetUI,
            () => EditingEmail
        );
        ApplyEmailCommand = new
        (
            async () => await ApplyEmailAsync(),
            () => EditingEmail
        );
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private void EnableEmailEditing()
    {
        EditingEmail = true;
        EmailInput?.ChangeEmailTextBoxMode(true);
    }

    private void ResetUI()
    {
        EditingEmail = false;
        EmailInput?.Reset();
        //EmailInput?.ChangeEmailTextBoxMode(false); //Not necessary, because the Contentcontrol covers the setback
    }

    private async Task ApplyEmailAsync()
    {
        var success = await ApplyEmailInput(EmailInput);
        if (!success) return;

        ResetUI();
    }

    private Task<EmailService>? loginTask = null;
    private async Task<bool> ApplyEmailInput(EmailInput emailInput)
    {
        EmailService? serviceInMaking;
        if (loginTask == null || loginTask.IsCompleted)
        {
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
                loginTask = null;
                return false;
            }
        }

        _session.EmailService = serviceInMaking;

        return true;
    }

    public async Task OnUnloadAsync()
    {
        if(SmsInput != null) await SmsInput.AwaitAllTasksAsync();
    }

    private void ResetData()
    {
        SmsInput?.ResetData();
        EncryptionPassphrase = SecurityVault.Instance.GetAesPassphrase().Value ?? string.Empty;
        //WebhookSigningKey = SecurityVault.Instance.GetWhSigningKey().Value ?? string.Empty;
    }

    private async Task SaveDataAsync()
    {
        CanApply_SmsSettings = false;

        var tasks = new List<Task>();

        if(SmsInput != null) tasks.Add(SmsInput.ConfirmParameterChangesAsync());

        //tasks.Add(WsClientService.Instance.UpdateWebhookSigningKey(WebhookSigningKey));
        //SecurityVault.Instance.SetWebsocketSigningKey(WebhookSigningKey);
        SecurityVault.Instance.SetGateWayEncryptionPhrase(EncryptionPassphrase);

        await Task.WhenAll(tasks);

        CanApply_SmsSettings = true;
    }
}
