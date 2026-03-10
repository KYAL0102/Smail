using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    public MainWindow(Task? serverTask = null)
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(this, serverTask);
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }
    
    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }

    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        // Optional: prevent multiple calls
        Closing -= MainWindow_Closing;

        // Give the ViewModel a chance to clean up
        await _viewModel.OnShutdownAsync();
    }
}