using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using SmailAvalonia.Services;
using SmailAvalonia.Views;

namespace SmailAvalonia.ViewModels;

public partial class RecipientConfigurationViewModel: ViewModelBase
{
    private Window _window;
    private SecurityVault _securityVault;
    private Session _session;
    private RecipientConfiguration _userControl;

    private int _currentBatch = 0;
    private const int BATCH_SIZE = 10;
    private string _batchPositionLabel = "";
    public string BatchPositionLabel 
    {
        get => _batchPositionLabel;
        set
        {
            _batchPositionLabel = value;
            OnPropertyChanged();
        }
    }
    private int _currentStartIndex = 0;
    private int CurrentStartIndex
    {
        get => _currentStartIndex;
        set
        {
            _currentStartIndex = value;
            OnPropertyChanged();

            PreviousBatchCommand.NotifyCanExecuteChanged();
            NextBatchCommand.NotifyCanExecuteChanged();
        }
    }

    private List<Contact> AllContacts { get; } = [];
    public ObservableCollection<Contact> Contacts { get; } = [];
    [ObservableProperty] private DataGridLength _nameColumnWidth = new(0);
    [ObservableProperty] private DataGridLength _mobileColumnWidth = new(0);
    [ObservableProperty] private DataGridLength _emailColumnWidth = new(0);
    [ObservableProperty] private DataGridLength _sentByColumnWidth = new(0);
    [ObservableProperty] private DataGridLength _payedByColumnWidth = new(0);
    [ObservableProperty] private DataGridLength _countryColumnWidth = new(0);
    [ObservableProperty] private DataGridLength _regionColumnWidth = new(0);
    [ObservableProperty] private DataGridLength _preferenceColumnWidth = new(0);

    public string SortMemberPath_Name { get; } = "Name";
    public string SortMemberPath_MobileNumber { get; } = "MobileNumber";
    public string SortMemberPath_Email { get; } = "Email";
    public string SortMemberPath_Country { get; } = "Country";
    public string SortMemberPath_Region { get; } = "Region";
    public string SortMemberPath_SentBy { get; } = "SentBy";
    public string SortMemberPath_PayedBy { get; } = "PayedBy";
    public string SortMemberPath_ContactPreference { get; } = "ContactPreference";
    private bool _isDataSourceSet = false;
    public bool IsDataSourcSet
    {
        get => _isDataSourceSet;
        set
        {
            _isDataSourceSet = value;
            LoadFromSourceCommand.NotifyCanExecuteChanged();
        }
    }

    public RelayCommand PreviousBatchCommand { get; init; }
    public RelayCommand NextBatchCommand { get; init; }
    public RelayCommand AddSingleContactCommand { get; init; }
    public RelayCommand<Contact> RemoveContactCommand { get; init; }
    public RelayCommand PickFileForImport { get; init; }
    public RelayCommand LoadFromSourceCommand { get; init; }
    public RelayCommand ClearPoolCommand { get; init; }
    public RelayCommand ContinueToMessageConfig { get; init; }

    public RecipientConfigurationViewModel(Window window, RecipientConfiguration userControl, Session session)
    {
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();
        _window = window;
        _session = session;
        _userControl = userControl;

        PreviousBatchCommand = new(
            PrevPage,
            () => CurrentStartIndex > 0
        );
        NextBatchCommand = new(
            NextPage,
            () => (CurrentStartIndex + (BATCH_SIZE - 1)) < AllContacts.Count - 1
        );
        AddSingleContactCommand = new(
            async() => await AddSingleNewContactToListAsync(),
            () => true
        );
        RemoveContactCommand = new(
            RemoveContact,
            _ => true
        );
        PickFileForImport = new(
            async () => await PickFileAsync(),
            () => true
        );
        LoadFromSourceCommand = new
        (
            async () => await LoadFromSourceAsync(),
            () => _isDataSourceSet
        );
        ClearPoolCommand = new(ClearRecepientPool);
        ContinueToMessageConfig = new(
            ContinueToMessageConfiguration
        );

        IsDataSourcSet = !string.IsNullOrEmpty(_securityVault.RecipientBasePath);
        Messenger.Subscribe(Globals.NewRecepientPoolBaseSourcePath, message => 
        {
            if (message.Data is string newPath) IsDataSourcSet = !string.IsNullOrEmpty(newPath);
            else Console.WriteLine("New path is not a string.");
        });

        AllContacts.Clear();
        if(_session.Payload != null) AllContacts.AddRange(_session.Payload.ContactPool.Keys);

        SetLastBatchAsCurrent();
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private void ClearRecepientPool()
    {
        AllContacts.Clear();
        SetLastBatchAsCurrent();
    }

    private async Task LoadFromSourceAsync()
    {
        var list = await RecipientPoolBaseLoader.LoadFromSourceAsync(_securityVault.HttpsThumbprint);
        list?
            .ForEach(AllContacts.Add);

        SetLastBatchAsCurrent();
    }

    private void SetLastBatchAsCurrent()
    {
        _currentBatch = (AllContacts.Count - 1) / BATCH_SIZE;
        UpdateCurrentBatch();
    }

    private void UpdateCurrentBatch()
    {
        // 1. Boundary Protection: Ensure page isn't out of range after deletions
        int maxBatch = Math.Max(0, (AllContacts.Count - 1) / BATCH_SIZE);
        if (_currentBatch > maxBatch) _currentBatch = maxBatch;

        // 2. Calculate Start Index based on page
        CurrentStartIndex = _currentBatch * BATCH_SIZE;

        // 3. Update the ObservableCollection
        Contacts.Clear();
        
        var items = AllContacts
            .Skip(CurrentStartIndex)
            .Take(BATCH_SIZE)
            .ToList();

        foreach (var item in items)
        {
            Contacts.Add(item);
        }
        
        BatchPositionLabel = $"{_currentBatch + 1}/{maxBatch + 1}";
        CheckColumnVisibility();
    }
    
    private void NextPage()
    {
        if ((_currentBatch + 1) * BATCH_SIZE < AllContacts.Count)
        {
            _currentBatch++;
            UpdateCurrentBatch();
        }
    }

    private void PrevPage()
    {
        if (_currentBatch > 0)
        {
            _currentBatch--;
            UpdateCurrentBatch();
        }
    }

    private async Task AddSingleNewContactToListAsync()
    {
        var dialogWindow = new Window
        {
            Title = "Create new Contact",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 600, Height = 450
        };

        var control = new ContactCreationControl(dialogWindow);
        dialogWindow.Content = control;

        var contactResult = await dialogWindow.ShowDialog<Contact>(_window);
        
        if (contactResult == null) return;

        AllContacts.Add(contactResult);
        SetLastBatchAsCurrent();
    }

    private void RemoveContact(Contact? contact)
    {
        if (contact == null) return;

        AllContacts.Remove(contact);
        UpdateCurrentBatch();
    }

    public async Task PickFileAsync()
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
                var list = await RecipientPoolBaseLoader.GetFromFileAsync(filePath);

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                {
                    list.ForEach(AllContacts.Add);
                    SetLastBatchAsCurrent();
                });
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
        }
    }

    private void CheckColumnVisibility()
    {
        bool hasName = false, hasMobile = false, hasEmail = false, hasCountry = false,
            hasRegion = false, hasPref = false, hasSent = false, hasPayed = false;

        foreach (var c in AllContacts)
        {
            if (!hasName && !string.IsNullOrWhiteSpace(c.Name)) hasName = true;
            if (!hasMobile && !string.IsNullOrWhiteSpace(c.MobileNumber)) hasMobile = true;
            if (!hasEmail && !string.IsNullOrWhiteSpace(c.Email)) hasEmail = true;
            if (!hasCountry && !string.IsNullOrWhiteSpace(c.HomeCountry)) hasCountry = true;
            if (!hasRegion && !string.IsNullOrWhiteSpace(c.HomeRegion)) hasRegion = true;
            if (!hasPref && c.ContactPreference != TransmissionType.NONE) hasPref = true;
            if (!hasSent && !string.IsNullOrEmpty(c.SentBy)) hasSent = true;
            if (!hasPayed && !string.IsNullOrEmpty(c.PayedBy)) hasPayed = true;
            
            // Optional: break early if all are found
            if (hasName && hasMobile && hasEmail && hasCountry && hasRegion && hasPref && hasSent && hasPayed) break;
        }

        DataGridLength GetWidth(bool v) => v ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(0);

        NameColumnWidth = GetWidth(hasName);
        MobileColumnWidth = GetWidth(hasMobile);
        EmailColumnWidth = GetWidth(hasEmail);
        CountryColumnWidth = GetWidth(hasCountry);
        RegionColumnWidth = GetWidth(hasRegion);
        PreferenceColumnWidth = GetWidth(hasPref);
        SentByColumnWidth = GetWidth(hasSent);
        PayedByColumnWidth = GetWidth(hasPayed);
    }

    public void ContinueToMessageConfiguration()
    {
        FormatContacts();

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToMessageConfigurationAction
        });
    }

    private void FormatContacts()
    {
        Dictionary<Contact, TransmissionType> finallist = [];

        foreach(var contact in AllContacts)
        {
            finallist.Add(contact, TransmissionType.NONE);
        }

        /*
        var preferredType = TransmissionType.Email;

        foreach (var contact in AllContacts)
        {
            TransmissionType type;

            if (preferredType == TransmissionType.Email)
            {
                if (!string.IsNullOrWhiteSpace(contact.Email) && FormatChecker.IsValidEmail(contact.Email)) type = TransmissionType.Email;
                else if (!string.IsNullOrWhiteSpace(contact.MobileNumber) && FormatChecker.IsValidMobile(contact.MobileNumber)) type = TransmissionType.SMS;
                else type = TransmissionType.NONE;
            }
            else // preferredType == TransmissionType.SMS
            {
                if (!string.IsNullOrWhiteSpace(contact.MobileNumber) && FormatChecker.IsValidMobile(contact.MobileNumber)) type = TransmissionType.SMS;
                else if (!string.IsNullOrWhiteSpace(contact.Email) && FormatChecker.IsValidEmail(contact.Email)) type = TransmissionType.Email;
                else type = TransmissionType.NONE;
            }

            finallist[contact] = type;
        }*/

        if(_session.Payload != null) _session.Payload.ContactPool = finallist;
    }
}
