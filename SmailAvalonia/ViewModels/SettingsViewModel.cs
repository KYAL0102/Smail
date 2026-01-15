using System.Threading.Tasks;
using Core.Services;

namespace SmailAvalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    public SettingsViewModel()
    {

    }

    public async Task InitializeDataAsync()
    {
        await Task.CompletedTask;
    }
}
