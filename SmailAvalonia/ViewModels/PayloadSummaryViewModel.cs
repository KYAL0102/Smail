using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;

namespace SmailAvalonia.ViewModels;

public class PayloadSummaryViewModel: ViewModelBase
{
    private readonly Session _session;

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

    private int _smsContactsAmount = -1;
    public int SmsContactsAmount
    {
        get => _smsContactsAmount;
        set
        {
            _smsContactsAmount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SmsContactsSentence));
        }
    }
    public string SmsContactsSentence => $"{_smsContactsAmount} contacts will receive this via";

    private int _emailContactsAmount = -1;
    public int EmailContactsAmount
    {
        get => _emailContactsAmount;
        set
        {
            _emailContactsAmount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EmailContactsSentence));
        }
    }
    public string EmailContactsSentence => $"{_emailContactsAmount} contacts will receive this via";

    public RelayCommand BackToConfigurationCommand { get; set; }
    public RelayCommand ExecutePayloadCommand { get; set; }
    public PayloadSummaryViewModel(Session session)
    {
        _session = session;
        EvaluateAmountOfTransmissionTypes();

        BackToConfigurationCommand = new(
            BackToConfiguration
        );
        ExecutePayloadCommand = new(
            StartPayloadExecution
        );
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    public void StartPayloadExecution()
    {
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToExecutionAction
        });
    }

    private void BackToConfiguration()
    {
        Messenger.Publish(new Message{
            Action = Globals.NavigateToMessageConfigurationAction
        });
    }

    private void EvaluateAmountOfTransmissionTypes()
    {
        Message = _session.Payload.Message;
        SmsContactsAmount = _session.Payload.Contacts.Where(c => c.Value == TransmissionType.SMS).Count();
        EmailContactsAmount = _session.Payload.Contacts.Where(c => c.Value == TransmissionType.Email).Count();
    }
}
