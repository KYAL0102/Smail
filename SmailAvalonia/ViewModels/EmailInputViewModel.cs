using System.Threading.Tasks;

namespace SmailAvalonia.ViewModels;

public class EmailInputViewModel : ViewModelBase
{
    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            OnPropertyChanged();
        }
    }

    public EmailInputViewModel() {}

    public async Task InitializeDataAsync()
    {

    }
}
