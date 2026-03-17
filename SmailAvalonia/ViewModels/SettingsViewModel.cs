using System.Threading.Tasks;
using Core.Services;
using Core.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using SmailAvalonia.Services;
using Avalonia.Controls;
using SmailAvalonia.Views;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using Avalonia.Threading;
using Microsoft.Graph.Models;
using Velopack;

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

    private bool _newEmailIsProcessing = false;
    public bool IsManualProcessing
    {
        get => _newEmailIsProcessing;
        set
        {
            _newEmailIsProcessing = value;
            OnPropertyChanged();
            ApplyEmailCommand.NotifyCanExecuteChanged();
            CancelEmailEditingCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _updateBtnLocked = false;
    public bool UpdateBtnLocked
    {
        get => _updateBtnLocked;
        set
        {
            _updateBtnLocked = value;
            OnPropertyChanged();
            UpdateCommand.NotifyCanExecuteChanged();
        }
    }

    private string _currentVersion = string.Empty;
    public string CurrentVersion
    {
        get => _currentVersion;
        set
        {
            _currentVersion = value;
            OnPropertyChanged();
        }
    }

    private string _updateBtnContent = "Check for Update";
    public string UpdateBtnContent
    {
        get => _updateBtnContent;
        set
        {
            _updateBtnContent = value;
            OnPropertyChanged();
        }
    }

    private string _updateInfoText = string.Empty;
    public string UpdateInfoText
    {
        get => _updateInfoText;
        set
        {
            _updateInfoText = value;
            OnPropertyChanged();
        }
    }
    private int _updateProgress = 0;
    public int UpdateProgress
    {
        get => _updateProgress;
        set
        {
            _updateProgress = value;
            OnPropertyChanged();
        }
    }
    private bool _updateIsDownloading = false;
    public bool UpdateIsDownloading
    {
        get => _updateIsDownloading;
        set
        {
            _updateIsDownloading = value;
            OnPropertyChanged();
        }
    }

    private UpdateInfo? _updateInfo = null;

    public RelayCommand ResetDataCommand { get; set; }
    public RelayCommand SaveDataCommand { get; set; }

    public RelayCommand EditEmailCommand { get; set; }
    public RelayCommand CancelEmailEditingCommand { get; set; }
    public RelayCommand ApplyEmailCommand { get; set; }
    public RelayCommand UpdateCommand { get; set; }

    public SettingsViewModel(Session session, Window? window)
    {
        _session = session;
        _window = window;

        SmsInput = new(_session);
        EncryptionPassphrase = SecurityVault.Instance.GetAesPassphrase().Value ?? string.Empty;
        EmailInput = new(_session);

        CurrentVersion = UpdateChecker.GetCurrentVersion();

        ResetDataCommand = new(ResetData);
        SaveDataCommand = new
        (
            async () => await ApplySmsChangesAsync(),
            () => CanApply_SmsSettings
        );
        EditEmailCommand = new
        (
            EnableEmailEditing,
            () => !EditingEmail
        );
        CancelEmailEditingCommand = new
        (
            ResetEmailInput,
            () => EditingEmail && !IsManualProcessing
        );
        ApplyEmailCommand = new
        (
            async () => await ApplyEmailAsync(),
            () => EditingEmail && !IsManualProcessing
        );
        UpdateCommand = new
        (
            async() => await CheckForUpdatesAsync(),
            () => !UpdateBtnLocked
        );
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private async Task CheckForUpdatesAsync()
    {
        UpdateBtnLocked = true;
        try
        {
            if(_updateInfo == null)
            {
                var result = await UpdateChecker.CheckForUpdatesManualAsync();

                if(result == null || result.IsDowngrade) UpdateInfoText = "You currently have the newest version!";
                else
                {
                    UpdateInfoText = $"There is a new version ({result.TargetFullRelease.Version})!";
                    UpdateBtnContent = "Install upgrade";

                    _updateInfo = result;
                }
            }
            else
            {
                UpdateProgress = 0;
                UpdateIsDownloading = true;
                var progressReporter = new Progress<int>(value =>
                {
                    UpdateProgress = value;
                });

                await UpdateChecker.UpdateAsync(_updateInfo, progressReporter);
            }
        }
        catch(Exception ex)
        {
            UpdateInfoText = ex.Message;
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
        }
        UpdateBtnLocked = false;
    }

    private void EnableEmailEditing()
    {
        EditingEmail = true;
        EmailInput?.ChangeEmailTextBoxMode(true);
    }

    private void ResetEmailInput()
    {
        EditingEmail = false;
        IsManualProcessing = false;
        loginTask = null;
        EmailInput?.Reset();
        //EmailInput?.ChangeEmailTextBoxMode(false); //Not necessary, because the Contentcontrol covers the setback
    }

    private async Task ApplyEmailAsync()
    {
        try 
        {
            var success = await ApplyEmailInput(EmailInput);
            if (success)
            {
                ResetEmailInput();
            }
        }
        finally 
        {
            // Centralized state cleanup
            EditingEmail = false;
            IsManualProcessing = false;
        }
    }

    private Task<EmailService>? loginTask = null;
    private async Task<bool> ApplyEmailInput(EmailInput emailInput)
    {
        try 
        {
            if (loginTask == null || loginTask.IsCompleted)
            {
                loginTask = emailInput.ConfirmLoginAsync();
            }
            else
            {
                IsManualProcessing = true;
                emailInput.ConfirmManual(); 
            }

            _session.EmailService = await loginTask;
            return true;
        }
        catch (Exception)
        {
            IsManualProcessing = false;
            return false;
        }
        finally 
        {
            // Ensures the next attempt starts fresh
            loginTask = null; 
        }
    }

    public async Task OnUnloadAsync()
    {
        if(SmsInput != null) await SmsInput.AwaitAllTasksAsync();
    }

    private void ResetData()
    {
        SmsInput?.ResetData();
        EncryptionPassphrase = SecurityVault.Instance.GetAesPassphrase().Value ?? string.Empty;
    }

    private async Task ApplySmsChangesAsync()
    {
        CanApply_SmsSettings = false;

        var tasks = new List<Task>();

        if(SmsInput != null && _session.SmsService != null) tasks.Add(SmsInput.ConfirmParameterChangesAsync());
        else if (SmsInput != null)
        {
            var smsService = await SmsInput.CreateSmsServiceAsync();

            await Task.Delay(100);
            Dispatcher.UIThread.Post(() => 
            {
                _session.SmsService = smsService;
            });

            await SmsInput.AwaitAllTasksAsync();
        }

        SecurityVault.Instance.SetGateWayEncryptionPhrase(EncryptionPassphrase);

        await Task.WhenAll(tasks);

        CanApply_SmsSettings = true;
    }
}
