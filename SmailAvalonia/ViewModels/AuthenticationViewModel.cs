using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;

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

    public RelayCommand StartMessageConfigurationCommand { get; init; }
    public AuthenticationViewModel(MessagePayload? payload = null)
    {
        _payload = payload;
        StartMessageConfigurationCommand = new(
            StartMessageConfiguration,
            () => true
        );
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private void StartMessageConfiguration()
    {
        //TODO: Check if the IP is available and username + password are correct

        Messenger.Publish(new Message
        {
            Action = Globals.NewSessionAction,
            Data = new Session
            {
                SmsService = new(SgIP, SgUsername, SgPassword)
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
