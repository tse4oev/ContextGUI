---
name: mvvm-pattern
description: Правильная реализация MVVM паттерна с CommunityToolkit.Mvvm для ContextGUI
---

# MVVM Pattern Implementation Skill

## Применение
Используй при создании ViewModels, Commands, и связывании с Views.

## Базовая структура ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ContextGUI.Core.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRegistryService _registryService;
    private readonly ILoggingService _logger;

    public MainViewModel(
        IRegistryService registryService,
        ILoggingService logger)
    {
        _registryService = registryService;
        _logger = logger;
    }

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<ContextMenuItem> _contextMenuItems = new();

    [ObservableProperty]
    private ContextMenuItem? _selectedItem;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _showOnlyEnabled;

    // Computed property
    public IEnumerable<ContextMenuItem> FilteredItems => 
        ContextMenuItems.Where(item => 
            (string.IsNullOrEmpty(SearchQuery) || 
             item.DisplayName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
            (!ShowOnlyEnabled || item.IsEnabled)
        );

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredItems));
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadContextMenuItemsAsync()
    {
        try
        {
            var items = await Task.Run(() => _registryService.GetAllContextMenuItems());
            ContextMenuItems.Clear();
            foreach (var item in items)
                ContextMenuItems.Add(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки элементов меню");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteItemCommand))]
    private async Task DisableItemAsync(ContextMenuItem item)
    {
        var result = await _registryService.DisableContextMenuItemAsync(item.RegistryPath);
        if (result.Success)
            item.IsEnabled = false;
    }

    private bool CanExecuteItemCommand() => SelectedItem != null;

    #endregion
}
```

## Правила для MVVM в ContextGUI

1. **Никогда не обращайся к View из ViewModel** - используй события или Messenger
2. **Все async операции в Commands** должны иметь суффикс Async
3. **Всегда используй CanExecute** для команд которые зависят от состояния
4. **ObservableCollection** для коллекций которые меняются
5. **Partial methods** OnPropertyChanged для побочных эффектов
6. **Services через DI** в конструкторе ViewModel

## Dependency Injection Setup

### App.xaml.cs
```csharp
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<ILoggingService, LoggingService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```
