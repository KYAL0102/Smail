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
    private Session _session;
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
    public MessageConfigurationViewModel(UserControl userControl, Session session)
    {
        _userControl = userControl;
        _session = session;
        
        ContinueToSummaryCommand = new(
            ContinueToPayloadSummary,
            () => true
        );
        OneStepBack = new(
            NavigateToPreviousStep
        );

        Message = _session.Payload.Message;
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private void ContinueToPayloadSummary()
    {
        _session.Payload.Message = Message;
        
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToPayloadSummaryAction
        });
    }

    private void NavigateToPreviousStep()
    {
        _session.Payload.Message = Message;

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToRecepientConfigurationAction
        });
    }
}
