using Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Core.ApiResponseClasses;

public class ContactSendStatus : INotifyPropertyChanged
{
    public required TransmissionType TransmissionType { get; init; }
    public required Contact Contact { get; init; }
    
    private SendStatus _status = SendStatus.PENDING;
    public required SendStatus Status 
    { 
        get => _status; 
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
