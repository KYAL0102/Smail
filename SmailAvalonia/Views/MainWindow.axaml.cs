using System;
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

    private bool _isShuttingDown = false;
    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_isShuttingDown) return;
        
        // 1. Prevent the window from closing immediately
        e.Cancel = true;

        // 2. Hide the window so it feels snappy to the user
        this.Hide();

        // 3. Run your cleanup
        try 
        {
            await _viewModel.OnShutdownAsync();
        }
        finally 
        {
            // 4. Set the flag and close for real
            _isShuttingDown = true;
            this.Close();
        }
    }
}