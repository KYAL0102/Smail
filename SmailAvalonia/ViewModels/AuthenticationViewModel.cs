using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;

namespace SmailAvalonia.ViewModels;

public class AuthenticationViewModel : ViewModelBase
{
    public RelayCommand StartMessageConfigurationCommand { get; init; }
    public AuthenticationViewModel()
    {
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
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToPayloadConfigurationAction,
            Data = new MessagePayload()
        });
    }
}
