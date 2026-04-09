using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;

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

    private string _subject = string.Empty;
    public string Subject
    {
        get => _subject;
        set
        {
            _subject = value;
            OnPropertyChanged();
        }
    }

    public MessageConfigurationViewModel(Session session)
    {
        _session = session;
        
        ContinueToSummaryCommand = new(
            ContinueToPayloadSummary,
            () => true
        );
        OneStepBack = new(
            NavigateToPreviousStep
        );

        Subject = _session.Payload.Subject;
        Message = _session.Payload.Message;
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private void ContinueToPayloadSummary()
    {
        _session.Payload.Subject = Subject;
        _session.Payload.Message = Message;
        
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToPayloadSummaryAction
        });
    }

    private void NavigateToPreviousStep()
    {
        _session.Payload.Subject = Subject;
        _session.Payload.Message = Message;

        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToRecepientConfigurationAction
        });
    }
}
