using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SmailAvalonia.ViewModels;

public class DataSettingsViewModel : ViewModelBase
{
    private UserControl _userControl;
    private SecurityVault _securityVault;

    public string RecipientpoolbasePathDescription { get; } = 
        "The path to an external source of data, which would be used as the base for every new payload. " + 
        "This can be a local path or an network URI, as long as the structure of the data is correct. " + 
        "Valid file types are CSV or XLSX with the following headers: Name, MobileNumber, Email, HomeCountry, HomeRegion, SentBy, PayedBy, ContactPreference. " +
        $"The order of these columns is not important. {Environment.NewLine}" +
        "In case for external URIs, the URI must lead to a valid GET-URI which returns a JSON. " + 
        "The needed structure is the same as for the CSV/XLSX path option.";

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
            PickFromLocalCommand.NotifyCanExecuteChanged();
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
    public RelayCommand PickFromLocalCommand { get; set; }

    public DataSettingsViewModel(UserControl userControl)
    {
        _userControl = userControl;
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();

        DataSourcePathInput = _securityVault.RecipientBasePath;

        EditSourcePathCommand = new
        (
            () => { EditingDataSource = true; },
            () => !EditingDataSource
        );
        CancelSourcePathEditingCommand = new
        (
            () => 
            { 
                EditingDataSource = false; 
                ResetDataSrcInput();
            },
            () => EditingDataSource
        );
        ApplySourcePathCommand = new
        (
            async () => await ApplySourcePathAsync(),
            () => EditingDataSource
        );
        ClearStorageCommand = new
        (
            async () => await ClearTokenStorageAsync()
        );
        PickFromLocalCommand = new(
            async () => await PickLocalFileForPoolSource(),
            () => EditingDataSource
        );
        ResetApplicationCommand = new(ResetApplication);
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private async Task PickLocalFileForPoolSource()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(_userControl);

            // Start async operation to open the dialog.
            var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Csv or Excel File",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("CSV and Excel Files")
                    {
                        Patterns = new[] { "*.csv", "*.xlsx", "*.xls" },
                        MimeTypes = new[]
                        {
                            "text/csv",
                            "application/vnd.ms-excel",
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        }
                    },
                    new("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count >= 1)
            {
                // Open reading stream from the first file.
                var filePath = files[0].Path.AbsolutePath;
                DataSourcePathInput = filePath;
                await ApplySourcePathAsync();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
        }
    }

    public void ResetDataSrcInput()
    {
        DataSourcePathInput = _securityVault.RecipientBasePath;
    }

    public async Task ApplySourcePathAsync()
    {
        EditingDataSource = false;
        DataSrcErrorMsg = string.Empty;
        var isValid = FormatChecker.GetDataSourceType(DataSourcePathInput) != DataSourceType.INVALID;

        if(!isValid && !string.IsNullOrEmpty(DataSourcePathInput))
        {
            EditingDataSource = true;
            DataSrcErrorMsg = "Input must the either an absolule local path or a valid URL!";
            return;
        }
        else if (!string.IsNullOrEmpty(DataSourcePathInput))
        {
            //TODO: Implement Thumbprint logic
            (bool success, string reason) = await NetworkManager.VerifySourceAsync(DataSourcePathInput);

            if(!success)
            {
                EditingDataSource = true;
                DataSrcErrorMsg = reason;
                return;
            }
        }

        _securityVault.RecipientBasePath = DataSourcePathInput;
        await _securityVault.SaveToFileAsync();
    }

    public async Task ClearTokenStorageAsync()
    {
        await _securityVault.ClearTokenPackageListAsync();
    }

    public void ResetApplication()
    {

    }
}
