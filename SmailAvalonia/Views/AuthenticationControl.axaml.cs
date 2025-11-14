using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class AuthenticationControl : UserControl
{
    private AuthenticationViewModel _viewModel;
    public AuthenticationControl()
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