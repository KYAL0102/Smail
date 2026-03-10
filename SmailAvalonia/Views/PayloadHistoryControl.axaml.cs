using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Core.Models;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class PayloadHistoryControl : UserControl
{
    private PayloadHistoryViewModel _viewModel;
    public PayloadHistoryControl(Dictionary<string, PayloadExecutionControl> history)
    {
        InitializeComponent();
        _viewModel = new(history);
        DataContext = _viewModel;
        Loaded += UserControl_Loaded;
    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}