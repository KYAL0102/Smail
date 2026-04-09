using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SmailAvalonia.ViewModels;

public class DataSettingsViewModel : ViewModelBase
{
    private SecurityVault _securityVault;

    private bool _editingDataSource = false;
    public bool EditingDataSource
    {
        get => _editingDataSource;
        set
        {
            _editingDataSource = value;
            OnPropertyChanged();
            EditSourcePathCommand.NotifyCanExecuteChanged();
            ApplySourcePathCommand.NotifyCanExecuteChanged();
            CancelSourcePathEditingCommand.NotifyCanExecuteChanged();
        }
    }

    private string _dataSourcePathInput = string.Empty;
    public string DataSourcePathInput
    {
        get => _dataSourcePathInput;
        set
        {
            _dataSourcePathInput = value;
            OnPropertyChanged();
        }
    }

    private string _dataSrcErrMsg = string.Empty;
    public string DataSrcErrorMsg
    {
        get => _dataSrcErrMsg;
        set
        {
            _dataSrcErrMsg = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand EditSourcePathCommand { get; set;}
    public RelayCommand CancelSourcePathEditingCommand { get; set; }
    public RelayCommand ApplySourcePathCommand { get; set; }
    public RelayCommand ClearStorageCommand { get; set; }
    public RelayCommand ResetApplicationCommand { get; set; }

    public DataSettingsViewModel()
    {
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();

        DataSourcePathInput = _securityVault.RecepientBasePath;

        EditSourcePathCommand = new
        (
            () => ReverseDataSourceEditingState(),
            () => !EditingDataSource
        );
        CancelSourcePathEditingCommand = new
        (
            () => ReverseDataSourceEditingState(),
            () => EditingDataSource
        );
        ApplySourcePathCommand = new
        (
            async () => await ApplySourcePath(),
            () => EditingDataSource
        );
        ClearStorageCommand = new
        (
            async () => await ClearTokenStorageAsync()
        );
        ResetApplicationCommand = new(ResetApplication);
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    public void ReverseDataSourceEditingState()
    {
        EditingDataSource = !EditingDataSource;

        if(!EditingDataSource) ResetDataSrcInput();
    }

    public void ResetDataSrcInput()
    {
        DataSourcePathInput = _securityVault.RecepientBasePath;
    }

    public async Task ApplySourcePath()
    {
        DataSrcErrorMsg = string.Empty;
        var isValid = FormatChecker.GetDataSourceType(DataSourcePathInput) != DataSourceType.INVALID;

        if(!isValid && !string.IsNullOrEmpty(DataSourcePathInput))
        {
            DataSrcErrorMsg = "Input must the either an absolule local path or a valid URL!";
            return;
        }
        else if (!string.IsNullOrEmpty(DataSourcePathInput))
        {
            (bool success, string reason) = await NetworkManager.VerifySourceAsync(DataSourcePathInput);

            if(!success)
            {
                DataSrcErrorMsg = reason;
                return;
            }
        }

        _securityVault.RecepientBasePath = DataSourcePathInput;
        await _securityVault.SaveToFileAsync();

        ReverseDataSourceEditingState();
    }

    public async Task ClearTokenStorageAsync()
    {
        await _securityVault.ClearTokenPackageListAsync();
    }

    public void ResetApplication()
    {

    }
}
