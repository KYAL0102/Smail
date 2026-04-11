using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;

namespace SmailAvalonia.ViewModels;

public partial class ContactCreationViewModel: ViewModelBase
{
    private Window _window;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _mobileNumber = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _region = string.Empty;
    [ObservableProperty] private string _sentBy = string.Empty;
    [ObservableProperty] private string _payedBy = string.Empty;
    public ObservableCollection<TransmissionType> ContactPreferences { get; } = new() 
    {
        TransmissionType.NONE,
        TransmissionType.SMS,
        TransmissionType.Email
    };
    [ObservableProperty] private TransmissionType _selectedContactPreference = TransmissionType.NONE;

    public RelayCommand CancelCommand { get; }
    public RelayCommand CreateCommand { get; }

    private bool _isContactInputValid = false;
    public bool IsContactInputValid
    {
        get => _isContactInputValid;
        set
        {
            _isContactInputValid = value;
            CreateCommand.NotifyCanExecuteChanged();
        }
    }
    public ContactCreationViewModel(Window window)
    {
        _window = window;

        CancelCommand = new(CancelOperation);
        CreateCommand = new(CreateContact);
    }

    public void CancelOperation() => _window.Close();

    public void CreateContact()
    {
        //TODO: Inform user about errors
        if(string.IsNullOrEmpty(Name)) return;
        if(!string.IsNullOrEmpty(MobileNumber) && !FormatChecker.IsValidMobile(MobileNumber)) return;
        if(!string.IsNullOrEmpty(Email) && !FormatChecker.IsValidEmail(Email)) return;

        var contact = new Contact
        {
            Name = Name,
            MobileNumber = MobileNumber,
            Email = Email,
            HomeCountry = Country,
            HomeRegion = Region,
            SentBy = SentBy,
            PayedBy = PayedBy,
            ContactPreference = SelectedContactPreference
        };

        _window.Close(contact);
    }
}
