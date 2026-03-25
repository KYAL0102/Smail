using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Services;
using Duende.IdentityModel.OidcClient;
using SmailAvalonia.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SmailAvalonia.ViewModels;

public class EmailInputViewModel : ViewModelBase
{
    private readonly EmailProviderService _providerService;
    private Session? _session;
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

    private bool _isManualUrlInputEditable = true;
    public bool IsManualUrlInputEditable
    {
        get => _isManualUrlInputEditable;
        set
        {
            _isManualUrlInputEditable = value;
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

    public EmailInputViewModel(Session? session = null) 
    {
        _providerService = App.ServiceProvider.GetRequiredService<EmailProviderService>();
        _session = session;
        Reset();
    }

    public async Task InitializeDataAsync()
    {

    }

    public void ConfirmManual()
    {
        IsManualUrlInputEditable = false;
        try
        {
            WebAuthenticationService.SetManualUrl(PastedURL);
        }
        catch(Exception e)
        {
            IsManualUrlInputEditable = true;
            ErrorMessage = e.Message;
            throw;
        }
    }

    public async Task<EmailService> ConfirmLoginAsync()
    {
        ErrorMessage = string.Empty;
        var provider = await _providerService.GetServerProviderFromEmailAsync(Email);

        if(provider != null)
        {
            var loginResult = await LoginViaOAuth(provider);
            return new EmailService(Email, loginResult, provider);
        }
        else throw new ArgumentException("Email-Provider was null.");
    }

    public void Reset()
    {
        if(_session != null)
        {
            if(_session.EmailService == null) Email = string.Empty;
            else Email = _session.EmailService.Email;
            IsEmailboxEditable = false;
        }
        else IsEmailboxEditable = true;

        ManualUrlInputVisible = false;
        PastedURL = string.Empty;
        
        _loginCts?.Cancel();
        _loginCts = null;
    }

    private async Task<LoginResult> LoginViaOAuth(Provider provider)
    {
        IsEmailboxEditable = false;
        ManualUrlInputVisible = true;
        IsManualUrlInputEditable = true;

        try
        {
            _loginCts = new();
            return await WebAuthenticationService.GetTokenFromUserWebPermissionAsync(provider, _loginCts.Token, Email);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsManualUrlInputEditable = true;
            Reset();

            Console.WriteLine($"Login failed: {ex.Message} - {ex.StackTrace}");
            throw;
        }
    }
}
