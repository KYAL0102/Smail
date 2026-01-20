using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SmailAvalonia.ViewModels;
using Core.Models;

namespace SmailAvalonia.Views;

public partial class SettingsControl : UserControl
{
    private SettingsViewModel _viewModel;
    public SettingsControl(Session session, Window? window = null)
    {
        InitializeComponent();
        _viewModel = new(session, window);
        DataContext = _viewModel;

        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}