using Avalonia.Controls;
using Avalonia.Interactivity;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(this);
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
    }
    
    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}