using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Services;
using Duende.IdentityModel.OidcClient;
using SmailAvalonia.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace SmailAvalonia.ViewModels;

public class EmailInputViewModel : ViewModelBase
{
    private readonly SecurityVault _securityVault;
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

    private bool _editingEmail = false;
    public bool EditingEmail 
    {
        get => _editingEmail;
        set
        {
            _editingEmail = value;
            OnPropertyChanged();
            ApplyEmailCommand.NotifyCanExecuteChanged();
            CancelEmailEditingCommand.NotifyCanExecuteChanged();
            EditEmailCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _canApply = false;
    public bool CanApply
    {
        get => _canApply;
        set
        {
            _canApply = value;
            OnPropertyChanged();
            ApplyEmailCommand.NotifyCanExecuteChanged();
            CancelEmailEditingCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _nativeBtnsVisible = true;
    public bool NativeButtonsVisible
    {
        get => _nativeBtnsVisible;
        set
        {
            _nativeBtnsVisible = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> EmailSuggestions { get; init; } = [];

    public RelayCommand EditEmailCommand { get; set; }
    public RelayCommand CancelEmailEditingCommand { get; set; }
    public RelayCommand ApplyEmailCommand { get; set; }

    public EmailInputViewModel(bool nativeButtonsVisible, Session? session = null) 
    {
        _securityVault = App.ServiceProvider.GetRequiredService<SecurityVault>();
        _providerService = App.ServiceProvider.GetRequiredService<EmailProviderService>();
        NativeButtonsVisible = nativeButtonsVisible;
        _session = session;

        EditEmailCommand = new
        (
            EnableEmailEditing,
            () => !EditingEmail
        );
        CancelEmailEditingCommand = new
        (
            ResetEmailInput,
            () => EditingEmail && CanApply
        );
        ApplyEmailCommand = new
        (
            async () => await ApplyEmailAsync(),
            () => EditingEmail && CanApply && nativeButtonsVisible
        );

        Reset();

        _securityVault.GetUsedEmails()
            .ForEach(EmailSuggestions.Add);
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
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
        var tokenPackage = _securityVault.GetPackageForEmail(Email);
        var provider = await _providerService.GetServerProviderFromEmailAsync(Email);

        if(provider == null) throw new ArgumentException("Email-Provider was null.");

        if(tokenPackage != null)
        {
            if(tokenPackage.AccessTokenExpiration < DateTimeOffset.UtcNow)
            {
                try
                {
                    var result = await WebAuthenticationService.RefreshPackageAsync(provider, tokenPackage.RefreshToken);

                    if(!result.IsError)
                    {
                        Console.WriteLine("Email-Accesstoken successfully renewed!");
                        _securityVault.UpdatePackageInListViaRefreshTokenResult(Email, result);
                        await _securityVault.SaveToFileAsync();
                        return new EmailService(tokenPackage, provider);
                    }
                    else Console.WriteLine($"Failed to renew accesstoken -> {result.ErrorDescription}");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Failed to renew accesstoken -> {ex.Message} - {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine("Email-Accesstoken is still valid!");
                return new EmailService(tokenPackage, provider);
            }
        }

        var loginResult = await LoginViaOAuth(provider);

        var package = new TokenPackage();
        package.Email = Email;
        package.AccessToken = loginResult.AccessToken;
        package.AccessTokenExpiration = loginResult.AccessTokenExpiration;
        package.RefreshToken = loginResult.RefreshToken;
            
        _securityVault.AddPackageToList(package);
        await _securityVault.SaveToFileAsync();
                
        return new EmailService(package, provider);
    }

    public void Reset()
    {
        if(_session != null)
        {
            if(_session.EmailService == null) Email = string.Empty;
            else Email = _session.EmailService.TokenPackage.Email;
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
        CanApply = true;
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

    private void EnableEmailEditing()
    {
        EditingEmail = true;
        CanApply = true;
        IsEmailboxEditable = true;
    }

    private void ResetEmailInput()
    {
        EditingEmail = false;
        CanApply = false;
        loginTask = null;
        Reset();
        //EmailInput?.ChangeEmailTextBoxMode(false); //Not necessary, because the Contentcontrol covers the setback
    }

    private async Task ApplyEmailAsync()
    {
        try 
        {
            var success = await ApplyEmailInput();
            if (success)
            {
                ResetEmailInput();
            }
        }
        finally 
        {
            // Centralized state cleanup
            EditingEmail = false;
            CanApply = false;
        }
    }

    private Task<EmailService>? loginTask = null;
    private async Task<bool> ApplyEmailInput()
    {
        try 
        {
            CanApply = false;
            if (loginTask == null || loginTask.IsCompleted)
            {
                loginTask = ConfirmLoginAsync();
            }
            else
            {
                ConfirmManual(); 
            }

            _session.EmailService = await loginTask;
            return true;
        }
        catch (Exception)
        {
            CanApply = false;
            return false;
        }
        finally 
        {
            // Ensures the next attempt starts fresh
            loginTask = null; 
        }
    }
}
