using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SmailAvalonia.ViewModels;

public class PayloadSummaryViewModel: ViewModelBase
{
    private readonly Session _session;

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

    public ObservableCollection<Channel> Channels { get; } = new() 
    { 
        new Channel{Name = "SMS", TransmissionType = TransmissionType.SMS}, 
        new Channel{Name = "Email", TransmissionType = TransmissionType.Email}
    };

    private Channel? _selectedChannel = null;
    public Channel? SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            _selectedChannel = value;
            ExecutePayloadCommand.NotifyCanExecuteChanged();
            EvaluateAmountOfTransmissionTypes();
            OnPropertyChanged();

            if(_session.Payload != null && _selectedChannel != null) 
                _session.Payload.PrimaryTransmissionType = _selectedChannel.TransmissionType;
        }
    }

    public ObservableCollection<TransmissionStrategy> Strategies { get; } = new()
    {
        new TransmissionStrategy { 
            Title = "Strict", 
            Key = (int) TransmissionStrategyKey.STRICT,
            Description = "Primary channel only. Skips contacts with missing data." 
        },
        new TransmissionStrategy { 
            Title = "Smart Fallback", 
            Key = (int) TransmissionStrategyKey.FALLBACK,
            Description = "Uses any available method if the primary one fails." 
        },
        new TransmissionStrategy { 
            Title = "Recipient Choice", 
            Key = (int) TransmissionStrategyKey.CHOICE,
            Description = "Follows contact preferences or primary channel if no preference." 
        }
    };

    private TransmissionStrategy? _selectedStrategy = null;
    public TransmissionStrategy? SelectedStrategy
    {
        get => _selectedStrategy;
        set
        {
            _selectedStrategy = value;
            ExecutePayloadCommand.NotifyCanExecuteChanged();
            EvaluateAmountOfTransmissionTypes();
            OnPropertyChanged();

            if(_session.Payload != null && _selectedStrategy != null) 
                _session.Payload.StrategyKey = (TransmissionStrategyKey) _selectedStrategy.Key;
        }
    }

    public record Channel
    {
        public string Name { get; set; } = string.Empty;
        public TransmissionType TransmissionType { get; set; } = TransmissionType.NONE;
    }

    public class TransmissionStrategy : ViewModelBase
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Key { get; set; } // Useful for logic checks

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set 
            {
                _isSelected = value;
                OnPropertyChanged();
            }
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

        BackToConfigurationCommand = new(
            BackToConfiguration
        );
        ExecutePayloadCommand = new(
            StartPayloadExecution,
            () => SelectedChannel != null && SelectedStrategy != null
        );

        SelectedChannel = Channels.SingleOrDefault(c => c.TransmissionType == session.Payload?.PrimaryTransmissionType);

        foreach (var strategy in Strategies)
        {
            if(session.Payload != null && strategy.Key == (int) session.Payload.StrategyKey)
            {
                strategy.IsSelected = true;
                SelectedStrategy = strategy;
            }
            strategy.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TransmissionStrategy.IsSelected) && ((TransmissionStrategy)s!).IsSelected)
                {
                    // Update the main property whenever an item is checked
                    SelectedStrategy = (TransmissionStrategy)s;
                }
            };
        }

        Subject = _session.Payload?.Subject ?? "";
        Message = _session.Payload?.Message ?? "";

        EvaluateAmountOfTransmissionTypes();
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
        var smsCount = 0;
        var emailCount = 0;

        var strategy = SelectedStrategy?.Key ?? -1;
        var primary = SelectedChannel?.TransmissionType ?? TransmissionType.NONE;

        if (_session.Payload != null && strategy != -1 && primary != TransmissionType.NONE)
        {
            ApplyLogicToContactList(strategy, primary);
            smsCount = _session.Payload.ContactPool.Count(c => c.Value == TransmissionType.SMS);
            emailCount = _session.Payload.ContactPool.Count(c => c.Value == TransmissionType.Email);
        }
        
        SmsContactsAmount = smsCount;
        EmailContactsAmount = emailCount;
    }

    private void ApplyLogicToContactList(int strategy, TransmissionType primary)
    {
        var contacts = _session.Payload?.ContactPool;
        if (contacts == null) return;

        foreach (var contact in contacts.Keys.ToList())
        {
            contacts[contact] = strategy switch
            {
                (int) TransmissionStrategyKey.STRICT   => GetStrictType(contact, primary),
                (int) TransmissionStrategyKey.FALLBACK => GetFallbackType(contact, primary),
                (int) TransmissionStrategyKey.CHOICE   => contact.ContactPreference != TransmissionType.NONE 
                                                        ? contact.ContactPreference 
                                                        : GetFallbackType(contact, primary),
                _                                       => TransmissionType.NONE
            };
        }

        // Local functions to encapsulate the decision logic
        TransmissionType GetStrictType(Contact c, TransmissionType p) => p switch
        {
            TransmissionType.SMS when FormatChecker.IsValidMobile(c.MobileNumber) => TransmissionType.SMS,
            TransmissionType.Email when FormatChecker.IsValidEmail(c.Email)     => TransmissionType.Email,
            _ => TransmissionType.NONE
        };

        TransmissionType GetFallbackType(Contact c, TransmissionType p)
        {
            bool hasValidSms = FormatChecker.IsValidMobile(c.MobileNumber);
            bool hasValidEmail = FormatChecker.IsValidEmail(c.Email);

            return p switch
            {
                TransmissionType.SMS   => hasValidSms ? TransmissionType.SMS : (hasValidEmail ? TransmissionType.Email : TransmissionType.NONE),
                TransmissionType.Email => hasValidEmail ? TransmissionType.Email : (hasValidSms ? TransmissionType.SMS : TransmissionType.NONE),
                _ => TransmissionType.NONE
            };
        }
    }
}
