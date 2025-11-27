using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Core.Models;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class PayloadExecutionControl : UserControl
{
    private PayloadExecutionViewModel _viewModel;
    public PayloadExecutionControl(Session session, MessagePayload payload)
    {
        InitializeComponent();
        _viewModel = new(session, payload);
        DataContext = _viewModel;
        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}