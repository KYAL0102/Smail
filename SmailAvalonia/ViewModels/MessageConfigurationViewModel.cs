using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmailAvalonia.ViewModels;

public class MessageConfigurationViewModel: ViewModelBase
{
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
    public ObservableCollection<Contact> Contacts { get; } = [];
    public RelayCommand PreviousBatchCommand { get; init; }
    public RelayCommand NextBatchCommand { get; init; }
    public RelayCommand AddSingleContactCommand { get; init; }
    public RelayCommand<Contact> RemoveContactCommand { get; init; }
    public RelayCommand PickFileForImport { get; init; }
    public RelayCommand ContinueToSummaryCommand { get; init; }

    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged();
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

    private UserControl _userControl { get; init; }
    public MessageConfigurationViewModel(UserControl userControl, MessagePayload? payload = null)
    {
        _userControl = userControl;
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
        ContinueToSummaryCommand = new(
            ContinueToPayloadSummary,
            () => true
        );

        if (payload != null)
        {
            Message = payload.Message;
            AllContacts.Clear();
            AllContacts.AddRange(payload.Contacts.Keys);
        }
    }

    public async Task InitializeDataAsync()
    {
        SetLastBatchAsCurrent();
        await Task.CompletedTask;
    }

    private void ContinueToPayloadSummary()
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

        var payload = new MessagePayload
        {
            Message = this.Message,
            Contacts = finallist
        };
        
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToPayloadSummaryAction,
            Data = payload
        });
    }

    private void SetLastBatchAsCurrent()
    {
        var lastBatchSize = AllContacts.Count % PageSize;
        if (lastBatchSize == 0) lastBatchSize = 5;
        var startIndex = AllContacts.Count - lastBatchSize;//for index and getting to the new batch
        CurrentStartIndex = startIndex;

        UpdateCurrentBatch(lastBatchSize);
    }

    private void UpdateCurrentBatch(int batchSize = -1)
    {
        if (CurrentStartIndex < 0 || CurrentStartIndex >= AllContacts.Count)
        {
            Console.WriteLine($"Index is out of bounds! ({CurrentStartIndex})");
            return;
        }
        Contacts.Clear();
        
        if (batchSize == -1)
        {
            batchSize = AllContacts.Count - CurrentStartIndex;
            if (batchSize > 5) batchSize = 5;
        }

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
        var newContact = new Contact
        {
            Name = NewContactName,
            MobileNumber = NewContactMobileNumber,
            Email = NewContactEmail
        };

        AllContacts.Add(newContact);
        SetLastBatchAsCurrent();
    }

    private void RemoveContact(Contact? contact)
    {
        if (contact == null) return;

        AllContacts.Remove(contact);
        SetLastBatchAsCurrent();
    }

    public async Task PickFileAsync()
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
            var file = files[0];
            var ext = Path.GetExtension(file.Name).ToLowerInvariant();
            await using var stream = await files[0].OpenReadAsync();

            var list = await Task.Run(async () =>
            {
                return await ImportController.FileContentToContactListAsync(stream, ext);
            });

            list
                .ForEach(AllContacts.Add);
            SetLastBatchAsCurrent();
        }
    }
}
