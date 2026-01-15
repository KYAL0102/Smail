using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class SettingsControl : UserControl
{
    private SettingsViewModel _viewModel;
    public SettingsControl()
    {
        InitializeComponent();
        _viewModel = new();
        DataContext = _viewModel;

        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}