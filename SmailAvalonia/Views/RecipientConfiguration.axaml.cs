using System;
using System.ComponentModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Core.Models;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class RecipientConfiguration : UserControl
{
    private RecipientConfigurationViewModel _viewModel;
    public RecipientConfiguration(Window window, Session session)
    {
        InitializeComponent();
        _viewModel = new(window, this, session);
        DataContext = _viewModel;

        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}