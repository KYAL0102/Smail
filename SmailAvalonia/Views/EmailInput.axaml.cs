using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Core.Models;
using Core.Services;
using Duende.IdentityModel.OidcClient;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class EmailInput : UserControl
{
    public EmailInputViewModel _viewModel;
    public EmailInput(Session? session = null)
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

    public void ChangeEmailTextBoxMode(bool isEditable)
    {
        _viewModel.IsEmailboxEditable = isEditable;
    }

    public async Task<EmailService> ConfirmLoginAsync()
    {
        return await _viewModel.ConfirmLoginAsync();
    }

    public void ConfirmManual()
    {
        _viewModel.ConfirmManual();
    }

    public void Reset()
    {
        _viewModel.Reset();
    }
}