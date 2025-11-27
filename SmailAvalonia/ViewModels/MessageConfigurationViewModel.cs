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
    private MessagePayload _payload;
    public RelayCommand ContinueToSummaryCommand { get; init; }
    public RelayCommand OneStepBack { get; init; }

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

    private UserControl _userControl { get; init; }
    public MessageConfigurationViewModel(UserControl userControl, MessagePayload payload)
    {
        _userControl = userControl;
        _payload = payload;
        
        ContinueToSummaryCommand = new(
            ContinueToPayloadSummary,
            () => true
        );
        OneStepBack = new(
            NavigateToPreviousStep
        );

        Message = payload.Message;
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private void ContinueToPayloadSummary()
    {
        _payload.Message = Message;
        
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToPayloadSummaryAction,
            Data = _payload
        });
    }

    private void NavigateToPreviousStep()
    {
        _payload.Message = Message;

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToRecepientConfigurationAction,
            Data = _payload
        });
    }
}
