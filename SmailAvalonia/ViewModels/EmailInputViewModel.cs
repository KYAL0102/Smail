using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Services;
using Duende.IdentityModel.OidcClient;
using SmailAvalonia.Services;

namespace SmailAvalonia.ViewModels;

public class EmailInputViewModel : ViewModelBase
{
    private CancellationTokenSource? _loginCts;
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

    private bool _isEmailboxEditable = true;
    public bool IsEmailboxEditable
    {
        get => _isEmailboxEditable;
        set
        {
            _isEmailboxEditable = value;
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

    public async Task<EmailService> ConfirmLoginAsync()
    {
        var provider = ProviderService.GetServerProviderFromEmail(Email);

        if(provider != null)
        {
            var loginResult = await LoginViaOAuth(provider);
            return new EmailService(loginResult, provider);
        }
        else throw new ArgumentException("Email-Provider was null.");
    }

    public void Reset()
    {
        IsEmailboxEditable = true;
        ManualUrlInputVisible = false;
        _loginCts?.Cancel();
        _loginCts = null;
    }

    private async Task<LoginResult> LoginViaOAuth(Provider provider)
    {
        IsEmailboxEditable = false;
        ManualUrlInputVisible = true;

        try
        {
            _loginCts = new();
            return await WebAuthenticationService.GetTokenFromUserWebPermissionAsync(provider, _loginCts.Token, Email);
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            throw;
        }
    }
}
