using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SmailAvalonia.ViewModels;
using Core.Models;

namespace SmailAvalonia.Views;

public partial class PayloadSummaryControl : UserControl
{
    private PayloadSummaryViewModel _viewModel;
    public PayloadSummaryControl(Session session)
    {
        InitializeComponent();
        _viewModel = new(session);
        DataContext = _viewModel;
        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}