using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmailAvalonia.ViewModels;

namespace SmailAvalonia.Views;

public partial class ContactCreationControl : UserControl
{
    private ContactCreationViewModel _viewModel;
    public ContactCreationControl(Window window)
    {
        InitializeComponent();
        _viewModel = new(window);
        DataContext = _viewModel;
    }
}