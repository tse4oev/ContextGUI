using System;
using System.Windows;
using System.Windows.Navigation;
using ContextGUI.Core.ViewModels;
using ContextGUI.Services.Interfaces;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ContextGUI.Views;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly IContentDialogService _contentDialogService;
    private readonly IAdminService _adminService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">Main view model.</param>
    /// <param name="contentDialogService">Content dialog service.</param>
    /// <param name="adminService">Admin service.</param>
    public MainWindow(MainViewModel viewModel, IContentDialogService contentDialogService, IAdminService adminService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _contentDialogService = contentDialogService;
        _adminService = adminService;
        DataContext = _viewModel;
        _contentDialogService.SetDialogHost(RootContentDialogPresenter);
        ResizeMode = ResizeMode.NoResize;
        Width = 1120;
        Height = 680;
        MinWidth = 1120;
        MinHeight = 680;
        MaxWidth = 1120;
        MaxHeight = 680;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_adminService.IsAdministrator())
        {
            await ShowAdminRequiredAsync();
        }

        await _viewModel.InitializeAsync();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Width != 1120 || Height != 680)
        {
            Width = 1120;
            Height = 680;
        }
    }

    private async System.Threading.Tasks.Task ShowAdminRequiredAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Требуются права администратора",
            Content = "Для редактирования контекстного меню необходимо запустить приложение от имени администратора.",
            PrimaryButtonText = "Ок",
            CloseButtonText = "Закрыть",
            DialogWidth = 460
        };

        await _contentDialogService.ShowAsync(dialog, System.Threading.CancellationToken.None);
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
