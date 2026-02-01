using System;
using System.IO;
using System.Windows;
using ContextGUI.Core.ViewModels;
using ContextGUI.Dialogs;
using ContextGUI.Services;
using ContextGUI.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Wpf.Ui;

namespace ContextGUI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<Views.MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        ConfigureLogging();

        services.AddSingleton<IAdminService, AdminService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IBackupHistoryService, BackupHistoryService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IRegistryWrapper, RegistryWrapper>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IShellRefreshService, ShellRefreshService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IUpdateService, GitHubUpdateService>();
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<IItemEditDialogService, ItemEditDialogService>();
        services.AddSingleton<IUpdateDialogService, UpdateDialogService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<Views.MainWindow>();
    }

    private static void ConfigureLogging()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ContextGUI",
            "Logs");

        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();
    }
}
