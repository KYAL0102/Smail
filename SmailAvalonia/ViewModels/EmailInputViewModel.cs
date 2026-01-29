using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Services;
using SmailAvalonia.Services;

namespace SmailAvalonia.ViewModels;

public class EmailInputViewModel : ViewModelBase
{
    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            OnPropertyChanged();
        }
    }

    private bool _manualInputVisible = false;
    public bool ManualUrlInputVisible 
    {
        get => _manualInputVisible;
        set
        {
            _manualInputVisible = value;
            OnPropertyChanged();
        }
    }

    private string _pastedURL = string.Empty;
    public string PastedURL
    {
        get => _pastedURL;
        set
        {
            _pastedURL = value;
            OnPropertyChanged();
        }
    }

    private string _errorMsg = string.Empty;
    public string ErrorMessage
    {
        get => _errorMsg;
        set
        {
            _errorMsg = value;
            OnPropertyChanged();
        }
    }

    public EmailInputViewModel() 
    {
        
    }

    public async Task InitializeDataAsync()
    {

    }

    public void ConfirmManual()
    {
        Console.WriteLine("Setting url manually...");
        try
        {
            WebAuthenticationService.SetManualUrl(PastedURL);
        }
        catch(Exception e)
        {
            ErrorMessage = e.Message;
            throw;
        }
    }

    public async Task ConfirmLoginAsync()
    {
        var provider = ProviderService.GetServerProviderFromEmail(Email);

        if(provider != null) await LoginViaOAuth(provider);
        else throw new ArgumentException("Email-Provider was null.");
    }

    public async Task LoginViaOAuth(Provider provider)
    {
        ManualUrlInputVisible = true;

        try
        {
            await WebAuthenticationService.GetTokenFromUserWebPermissionAsync(provider, Email);
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            throw;
        }
    }
}
