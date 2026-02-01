using System;
using System.Windows;
using System.Windows.Navigation;
using ContextGUI.Core.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ContextGUI.Views;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly IContentDialogService _contentDialogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">Main view model.</param>
    /// <param name="contentDialogService">Content dialog service.</param>
    public MainWindow(MainViewModel viewModel, IContentDialogService contentDialogService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _contentDialogService = contentDialogService;
        DataContext = _viewModel;
        _contentDialogService.SetDialogHost(RootContentDialogPresenter);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OnDonateLinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(info);
        }
        catch
        {
        }

        e.Handled = true;
    }
}
