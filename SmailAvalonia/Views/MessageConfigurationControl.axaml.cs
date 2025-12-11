using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Core.Models;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class MessageConfigurationControl : UserControl
{
    private MessageConfigurationViewModel _viewModel;
    public MessageConfigurationControl(MessagePayload payload)
    {
        InitializeComponent();
        _viewModel = new(this, payload);
        DataContext = _viewModel;
        Loaded += UserControl_Loaded;

    }

    private async void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDataAsync();
    }
}