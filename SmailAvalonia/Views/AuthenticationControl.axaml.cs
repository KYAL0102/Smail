using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Core.Models;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class AuthenticationControl : UserControl
{
    private AuthenticationViewModel _viewModel;
    public AuthenticationControl(Window? window = null)
    {
        InitializeComponent();
        _viewModel = new(window);
        DataContext = _viewModel;
        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}