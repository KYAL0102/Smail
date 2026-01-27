using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Core.Models;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia;

public partial class EmailInput : UserControl
{
    public EmailInputViewModel _viewModel;
    public EmailInput()
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

    public async Task ConfirmLoginAsync()
    {
        await _viewModel.ConfirmLoginAsync();
    }

    public void ConfirmManual()
    {
        _viewModel.ConfirmManual();
    }
}