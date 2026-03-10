using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Models;
using SmailAvalonia.Views;

namespace SmailAvalonia.ViewModels;

public class PayloadHistoryViewModel: ViewModelBase
{
    public ObservableCollection<PayloadItem> Payloads { get; set; } = 
    [
        /*new PayloadItem
        {
            Identifier = "Item 1 - 23:59:59"
        },
        new PayloadItem
        {
            Identifier = "Item 2 - 23:59:59"
        },
        new PayloadItem
        {
            Identifier = "Item 3 - 23:59:59"
        },
        new PayloadItem
        {
            Identifier = "Item 4 - 23:59:59"
        },
        new PayloadItem
        {
            Identifier = "Item 5 - 23:59:59"
        },
        new PayloadItem
        {
            Identifier = "Item 6 - 23:59:59"
        },
        new PayloadItem
        {
            Identifier = "Item 7 - 23:59:59"
        }*/
    ];

    private PayloadItem? _selectedItem = null;
    public PayloadItem? SelectedItem 
    { 
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();

            if(_selectedItem != null && _selectedItem.Control != null) 
                NavigateToChosenControl(_selectedItem.Control);
        }
    }

    public record PayloadItem
    {
        public string Identifier { get; set; } = string.Empty;
        public PayloadExecutionControl? Control = null;
    }

    public PayloadHistoryViewModel(Dictionary<string, PayloadExecutionControl> history) 
    {
        //if(history.Count != 0) Payloads.Clear();

        history
            .Select(kp => new PayloadItem
            {
                Identifier = kp.Key,
                Control = kp.Value
            })
            .ToList()
            .ForEach(Payloads.Add);
    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }

    private static void NavigateToChosenControl(PayloadExecutionControl control)
    {
        Messenger.Publish(new Message
        {
            Action = Globals.NavigateToExecutionAction,
            Data = control
        });
    }
}
