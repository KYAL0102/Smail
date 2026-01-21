using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Core.Models;
using Core.Services;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class SmsGatewayInput : UserControl
{
    private SmsGatewayInputViewModel _viewModel;
    public SmsGatewayInput(Session? session = null)
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

    public async Task<SmsService?> CreateSmsServiceAsync()
    {
        return await _viewModel.CreateSmsServiceAsync();
    }

    public async Task ConfirmParameterChangesAsync() => await _viewModel.ConfirmParameterChangeAsync();

    public void ResetData() => _viewModel.ResetData();

    public async Task AwaitAllTasksAsync() => await _viewModel.AwaitAllTasksAsync();
}