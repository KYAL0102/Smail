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
using Microsoft.Extensions.DependencyInjection;

namespace SmailAvalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SecurityVault _securityVault;
    private Session _session;
    private Window? _window;

    public EmailInput? EmailInput { get; } = null;

    public SmsGatewayInput? SmsInput { get; } = null;

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
    public RelayCommand UpdateCommand { get; set; }

    public SettingsViewModel(Session session, Window? window)
    {
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();
        _session = session;
        _window = window;

        SmsInput = new(true, true, _session);
        EmailInput = new(true, _session);

        CurrentVersion = UpdateChecker.GetCurrentVersion();

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

    public async Task OnUnloadAsync()
    {
        if(SmsInput != null) await SmsInput.AwaitAllTasksAsync();
    }
}
