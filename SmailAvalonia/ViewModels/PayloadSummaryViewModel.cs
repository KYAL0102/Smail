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

    private double _estimatedTimeForExecution = -1;
    public double EstimatedTimeForExecution
    {
        get => _estimatedTimeForExecution;
        set
        {
            _estimatedTimeForExecution = value;
            OnPropertyChanged();
        }
    }

    public MessagePayload Payload { get; init; }
    public RelayCommand BackToConfigurationCommand { get; set; }
    public RelayCommand ExecutePayloadCommand { get; set; }
    public PayloadSummaryViewModel(MessagePayload? payload = null)
    {
        Payload = payload == null ? new() : payload;

        BackToConfigurationCommand = new(
            BackToConfiguration
        );
        ExecutePayloadCommand = new(
            StartPayloadExecution
        );
    }

    public async Task InitializeDataAsync()
    {
        EvaluateAmountOfTransmissionTypes();
        await Task.CompletedTask;
    }

    public void StartPayloadExecution()
    {
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToExecutionAction,
            Data = Payload
        });
    }

    /// <summary>
    /// Estimates the time to send messages to all contacts.
    /// </summary>
    /// <param name="smsCount">Number of contacts to send SMS to.</param>
    /// <param name="emailCount">Number of contacts to send Email to.</param>
    /// <param name="smsDelayMs">Delay between SMS messages in milliseconds.</param>
    /// <param name="emailDelayMs">Delay between Email messages in milliseconds.</param>
    /// <returns>Estimated TimeSpan for the whole operation.</returns>
    public TimeSpan EstimateSendTime(int smsCount, int emailCount, int smsDelayMs = 200, int emailDelayMs = 500)
    {
        // Total milliseconds
        double totalMs = (smsCount * smsDelayMs) + (emailCount * emailDelayMs);

        return TimeSpan.FromMilliseconds(totalMs);
    }

    private void BackToConfiguration()
    {
        Messenger.Publish(new Message{
            Action = Globals.NavigateToMessageConfigurationAction,
            Data = Payload
        });
    }

    private void EvaluateAmountOfTransmissionTypes()
    {
        SmsContactsAmount = Payload.Contacts.Where(c => c.Value == TransmissionType.SMS).Count();
        EmailContactsAmount = Payload.Contacts.Where(c => c.Value == TransmissionType.Email).Count();
        EstimatedTimeForExecution = EstimateSendTime(SmsContactsAmount, EmailContactsAmount).Seconds;
    }
}
