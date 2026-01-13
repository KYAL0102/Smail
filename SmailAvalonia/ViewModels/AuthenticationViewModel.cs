using System;
using System.Threading.Tasks;
using System.Net.Http;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;

namespace SmailAvalonia.ViewModels;

public class AuthenticationViewModel : ViewModelBase
{
    private MessagePayload? _payload = null;

    private string _sgIp = string.Empty;
    public string SgIP
    {
        get => _sgIp;
        set
        {
            _sgIp = value;
            OnPropertyChanged();
        }
    }

    private string _sgPort = string.Empty;
    public string SgPort
    {
        get => _sgPort;
        set
        {
            _sgPort = value;
            OnPropertyChanged();
        }
    }

    private string _sgUsrName = string.Empty;
    public string SgUsername
    {
        get => _sgUsrName;
        set
        {
            _sgUsrName = value;
            OnPropertyChanged();
        }
    }

    private string _sgPwd = string.Empty;
    public string SgPassword
    {
        get => _sgPwd;
        set
        {
            _sgPwd = value;
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

    public RelayCommand StartMessageConfigurationCommand { get; init; }
    public AuthenticationViewModel(MessagePayload? payload = null)
    {
        _payload = payload;
        StartMessageConfigurationCommand = new(
            async() => await StartMessageConfiguration(),
            () => true
        );
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private async Task StartMessageConfiguration()
    {
        var parseSuccess = int.TryParse(SgPort, out var port);
        if (!parseSuccess) port = 8080;
        
        SmsService smsService = new(SgIP, port, SgUsername, SgPassword);
        //TODO: Check if the IP is available and username + password are correct
        try
        {
            var response = await smsService.IsDeviceReachableAsync();

            if (response == null || !response.IsSuccessStatusCode) 
            {
                if (response == null) ErrorMessage = "An unknown error occured.";
                else ErrorMessage = $"{response.ReasonPhrase} ({response.StatusCode})";
                return;
            }
        } 
        catch (Exception e) 
        {
            //Console.WriteLine(e.Message);
            ErrorMessage = e.Message;
            return;
        }

        _ = Task.Run(smsService.RegisterWebhooks);

        Messenger.Publish(new Message
        {
            Action = Globals.NewSessionAction,
            Data = new Session
            {
                SmsService = smsService
            }
        });
        SgUsername = string.Empty;
        SgPassword = string.Empty;

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToRecepientConfigurationAction,
            Data = _payload ?? new MessagePayload()
        });
    }
}
