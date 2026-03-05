using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Core.Models;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class RecepientConfiguration : UserControl
{
    private RecepientConfigurationViewModel _viewModel;
    public RecepientConfiguration(Session session)
    {
        InitializeComponent();
        _viewModel = new(this, session);
        DataContext = _viewModel;

        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}