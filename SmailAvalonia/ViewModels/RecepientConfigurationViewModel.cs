using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using SmailAvalonia.Views;

namespace SmailAvalonia.ViewModels;

public partial class RecepientConfigurationViewModel: ViewModelBase
{
    private SecurityVault _securityVault;
    private Session _session;
    private RecepientConfiguration _userControl;
    private List<Contact> AllContacts { get; } = new()
    {
        new Contact{ Name = "Yannik Lisa", MobileNumber = "+436508318025"},
        new Contact{ Name = "Enterich Duck", Email = "enterichtheduck@gmail.com"}
    };

    private const int PageSize = 5;
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

    private string _newContactName = string.Empty;
    public string NewContactName
    {
        get => _newContactName;
        set
        {
            _newContactName = value;
            OnPropertyChanged();
        }
    }

    private string _newContactMobileNumber = string.Empty;
    public string NewContactMobileNumber
    {
        get => _newContactMobileNumber;
        set
        {
            _newContactMobileNumber = value;
            OnPropertyChanged();
        }
    }

    private string _newContactEmail = string.Empty;
    public string NewContactEmail
    {
        get => _newContactEmail;
        set
        {
            _newContactEmail = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Contact> Contacts { get; } = [];
    [ObservableProperty] private bool _isNameVisible = false;
    [ObservableProperty] private bool _isMobileVisible = false;
    [ObservableProperty] private bool _isEmailVisible = false;
    [ObservableProperty] private bool _isSentByVisible = false;
    [ObservableProperty] private bool _isPayedByVisible = false;
    [ObservableProperty] private bool _isRegionVisible = false;
    [ObservableProperty] private bool _isPreferenceVisible = false;
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
    public RelayCommand OneStepBack { get; init; }

    public RecepientConfigurationViewModel(RecepientConfiguration userControl, Session session)
    {
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();
        _userControl = userControl;
        _session = session;

        PreviousBatchCommand = new(
            PrevPage,
            () => CurrentStartIndex > 0
        );
        NextBatchCommand = new(
            NextPage,
            () => (CurrentStartIndex + (PageSize - 1)) < AllContacts.Count - 1
        );
        AddSingleContactCommand = new(
            AddSingleNewContactToList,
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
        OneStepBack = new(
            NavigateOneStepBack
        );

        IsDataSourcSet = !string.IsNullOrEmpty(_securityVault.RecepientBasePath);
        Messenger.Subscribe(Globals.NewRecepientPoolBaseSourcePath, message => 
        {
            if (message.Data is string newPath) IsDataSourcSet = !string.IsNullOrEmpty(newPath);
            else Console.WriteLine("New path is not a string.");
        });

        AllContacts.Clear();
        AllContacts.AddRange(_session.Payload.ContactPool.Keys);
    }

    public async Task InitializeDataAsync()
    {
        SetLastBatchAsCurrent();
        await Task.CompletedTask;
    }

    private void ClearRecepientPool()
    {
        AllContacts.Clear();
        SetLastBatchAsCurrent();
    }

    private async Task LoadFromSourceAsync()
    {
        var path = _securityVault.RecepientBasePath;
        var type = FormatChecker.GetDataSourceType(path);
        var storageProvider = TopLevel.GetTopLevel(_userControl)?.StorageProvider;

        if(type == DataSourceType.INVALID || storageProvider == null)
        {
            Console.WriteLine($"type is invalid or storageProvider is null.");
            return; //TODO: POPUP that path is not valid (should not occur)
        }

        List<Contact>? list = null;
        if (type == DataSourceType.LOCAL)
        {
            var file = await storageProvider.TryGetFileFromPathAsync(path);
            if(file == null)
            {
                Console.WriteLine($"File on {path} does not exist.");
                return;
            }
            list = await GetFromFileAsync(file);

        }
        else if (type == DataSourceType.URI)
        {
            list = await NetworkManager.FetchFromUriAsync(path);
        }

        list?
            .ForEach(AllContacts.Add);

        SetLastBatchAsCurrent();
    }

    private void SetLastBatchAsCurrent()
    {
        var lastBatchSize = AllContacts.Count % PageSize;
        if (lastBatchSize == 0) lastBatchSize = 5;
        var startIndex = AllContacts.Count - lastBatchSize;//for index and getting to the new batch
        CurrentStartIndex = startIndex;

        UpdateCurrentBatch(lastBatchSize);
        CheckColumnVisibility();
    }

    private void UpdateCurrentBatch(int batchSize = -1)
    {
        Contacts.Clear();
        if (CurrentStartIndex < 0 || CurrentStartIndex >= AllContacts.Count)
        {
            Console.WriteLine($"Index is out of bounds! ({CurrentStartIndex})");
            return;
        }

        if(batchSize == -1) batchSize = PageSize;

        AllContacts
            .Skip(CurrentStartIndex)
            .Take(batchSize)
            .ToList()
            .ForEach(Contacts.Add);
    }
    
    private void NextPage()
    {
        CurrentStartIndex += 5;
        UpdateCurrentBatch();
    }

    private void PrevPage()
    {
        CurrentStartIndex -= 5;
        UpdateCurrentBatch();
    }

    private void AddSingleNewContactToList()
    {
        if(!FormatChecker.IsValidMobile(NewContactMobileNumber) ||
            !FormatChecker.IsValidEmail(NewContactEmail))
        {
            //TODO: Pop-up telling the user the error
            return;
        }
        
        var newContact = new Contact
        {
            Name = NewContactName,
            MobileNumber = NewContactMobileNumber,
            Email = NewContactEmail
        };

        AllContacts.Add(newContact);
        ResetSingleContactInputFields();
        SetLastBatchAsCurrent();
    }

    private void ResetSingleContactInputFields()
    {
        NewContactName = string.Empty;
        NewContactEmail = string.Empty;
        NewContactMobileNumber = string.Empty;
    }

    private void RemoveContact(Contact? contact)
    {
        if (contact == null) return;

        AllContacts.Remove(contact);
        SetLastBatchAsCurrent(); //TODO: Stay on current batch
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
                var list = await GetFromFileAsync(files[0]);

                list
                    .ForEach(AllContacts.Add);
                
                SetLastBatchAsCurrent();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
        }
    }

    private async Task<List<Contact>> GetFromFileAsync(IStorageFile file)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        await using var stream = await file.OpenReadAsync();

        return await Task.Run(async () =>
        {
            return await ImportController.FileContentToContactListAsync(stream, ext);
            //TODO: Pop-up about state of import (all successfull; unvalid rows/cells?)
        });
    }

    private void CheckColumnVisibility()
    {
        IsNameVisible = AllContacts.Any(c => !string.IsNullOrWhiteSpace(c.Name));
        IsMobileVisible = AllContacts.Any(c => !string.IsNullOrWhiteSpace(c.MobileNumber));
        IsEmailVisible = AllContacts.Any(c => !string.IsNullOrWhiteSpace(c.Email));
        IsRegionVisible = AllContacts.Any(c => !string.IsNullOrWhiteSpace(c.HomeRegion));
        IsPreferenceVisible = AllContacts.Any(c => c.ContactPreference != TransmissionType.NONE);
        IsSentByVisible = AllContacts.Any(c => !string.IsNullOrEmpty(c.SentBy));
        IsPayedByVisible = AllContacts.Any(c => !string.IsNullOrEmpty(c.PayedBy));
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
        }

        _session.Payload.ContactPool = finallist;
    }

    private void NavigateOneStepBack()
    {
        FormatContacts();

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToAuthenticationAction
        });
    }
}
