using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContextGUI.Models;
using ContextGUI.Services.Interfaces;

namespace ContextGUI.Core.ViewModels;

/// <summary>
/// Main view model for the application shell.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IRegistryService _registryService;
    private readonly IItemEditDialogService _itemEditDialogService;
    private readonly IShellRefreshService _shellRefreshService;
    private readonly IAdminService _adminService;
    private readonly ILoggingService _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="registryService">Registry service.</param>
    /// <param name="itemEditDialogService">Item edit dialog service.</param>
    /// <param name="shellRefreshService">Shell refresh service.</param>
    /// <param name="logger">Logging service.</param>
    public MainViewModel(
        IRegistryService registryService,
        IItemEditDialogService itemEditDialogService,
        IShellRefreshService shellRefreshService,
        IAdminService adminService,
        ILoggingService logger)
    {
        _registryService = registryService;
        _itemEditDialogService = itemEditDialogService;
        _shellRefreshService = shellRefreshService;
        _adminService = adminService;
        _logger = logger;
        ContextMenuItems.CollectionChanged += (_, _) => ApplyFilters();
        ApplyFilters();
    }

    [ObservableProperty]
    private ObservableCollection<ContextMenuItem> _contextMenuItems = new();

    public ObservableCollection<ContextMenuItem> FilteredItems { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DisableItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(EnableItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditItemCommand))]
    private ContextMenuItem? _selectedItem;

    [ObservableProperty]
    private string _selectedItemNotice = string.Empty;

    [ObservableProperty]
    private bool _showSelectedItemNotice;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _showOnlyEnabled;

    [ObservableProperty]
    private ContextMenuCategory _selectedCategory = ContextMenuCategory.All;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showStatusMessage;

    /// <summary>
    /// Loads initial data for the main view.
    /// </summary>
    public Task InitializeAsync() => LoadContextMenuItemsAsync();

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnShowOnlyEnabledChanged(bool value)
    {
        ApplyFilters();
    }

    partial void OnSelectedCategoryChanged(ContextMenuCategory value)
    {
        ApplyFilters();
    }

    partial void OnSelectedItemChanged(ContextMenuItem? value)
    {
        if (value == null)
        {
            SelectedItemNotice = string.Empty;
            ShowSelectedItemNotice = false;
            return;
        }

        if (value.IsSystemItem)
        {
            SelectedItemNotice = "Системный элемент: редактирование и удаление недоступны.";
            ShowSelectedItemNotice = true;
            return;
        }

        if (value.IsLegacyHandler)
        {
            SelectedItemNotice = "Legacy-обработчик: редактирование недоступно, можно только включать/отключать.";
            ShowSelectedItemNotice = true;
            return;
        }

        if (value.IsModernHandler)
        {
            SelectedItemNotice = "Современный обработчик: редактирование недоступно.";
            ShowSelectedItemNotice = true;
            return;
        }

        SelectedItemNotice = string.Empty;
        ShowSelectedItemNotice = false;
    }

    [RelayCommand]
    private async Task LoadContextMenuItemsAsync()
    {
        try
        {
            if (!_adminService.IsAdministrator())
            {
                ContextMenuItems.Clear();
                ApplyFilters();
                SetStatus("Запустите приложение от имени администратора для редактирования контекстного меню.");
                return;
            }

            var items = await _registryService.GetAllContextMenuItemsAsync();
            ContextMenuItems.Clear();
            foreach (var item in items)
            {
                ContextMenuItems.Add(item);
            }

            ApplyFilters();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to load context menu items");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadContextMenuItemsAsync();
        if (_adminService.IsAdministrator())
        {
            SetStatus("Список обновлен.");
        }
    }

    private bool CanDeleteSelected() => SelectedItem is { IsSystemItem: false };

    private bool CanDisableSelected() => SelectedItem is { IsEnabled: true };

    private bool CanEnableSelected() => SelectedItem is { IsEnabled: false };

    private bool CanEditSelected() => SelectedItem is { IsSystemItem: false, IsLegacyHandler: false, IsModernHandler: false };

    [RelayCommand(CanExecute = nameof(CanDisableSelected))]
    private async Task DisableItemAsync()
    {
        if (SelectedItem == null)
        {
            return;
        }

        var result = await _registryService.DisableContextMenuItemAsync(SelectedItem.RegistryPath);
        if (result.Success)
        {
            SelectedItem.IsEnabled = false;
            ApplyFilters();
            NotifyShellChange();
        }
        else
        {
            _logger.LogWarning("Disable failed: {0}", result.Error ?? "Unknown error");
            SetStatus(result.Error ?? "Не удалось отключить элемент.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanEnableSelected))]
    private async Task EnableItemAsync()
    {
        if (SelectedItem == null)
        {
            return;
        }

        var result = await _registryService.EnableContextMenuItemAsync(SelectedItem.RegistryPath);
        if (result.Success)
        {
            SelectedItem.IsEnabled = true;
            ApplyFilters();
            NotifyShellChange();
        }
        else
        {
            _logger.LogWarning("Enable failed: {0}", result.Error ?? "Unknown error");
            SetStatus(result.Error ?? "Не удалось включить элемент.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteItemAsync()
    {
        if (SelectedItem == null)
        {
            return;
        }

        if (SelectedItem.IsSystemItem)
        {
            _logger.LogWarning("Attempted to delete system item: {0}", SelectedItem.RegistryPath);
            return;
        }

        var target = SelectedItem;
        var result = await _registryService.DeleteContextMenuItemAsync(target.RegistryPath);
        if (result.Success)
        {
            ContextMenuItems.Remove(target);
            SelectedItem = null;
            ApplyFilters();
            NotifyShellChange();
        }
        else
        {
            _logger.LogWarning("Delete failed: {0}", result.Error ?? "Unknown error");
            SetStatus(result.Error ?? "Не удалось удалить элемент.");
        }
    }

    private void ApplyFilters()
    {
        var query = SearchQuery?.Trim() ?? string.Empty;
        var filtered = ContextMenuItems.Where(item =>
            MatchesSearch(item, query) &&
            MatchesEnabledFilter(item) &&
            MatchesCategory(item));

        FilteredItems.Clear();
        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }

        if (SelectedItem != null && !FilteredItems.Contains(SelectedItem))
        {
            SelectedItem = null;
        }
    }

    private bool MatchesSearch(ContextMenuItem item, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return item.DisplayName.Contains(query, System.StringComparison.OrdinalIgnoreCase)
               || item.Name.Contains(query, System.StringComparison.OrdinalIgnoreCase)
               || (item.Command?.Contains(query, System.StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool MatchesEnabledFilter(ContextMenuItem item)
    {
        return !ShowOnlyEnabled || item.IsEnabled;
    }

    private bool MatchesCategory(ContextMenuItem item)
    {
        return SelectedCategory switch
        {
            ContextMenuCategory.All => true,
            ContextMenuCategory.Files => item.Category == "All Files",
            ContextMenuCategory.Folders => item.Category == "Folder",
            ContextMenuCategory.FolderBackground => item.Category == "Folder Background",
            ContextMenuCategory.Drives => item.Category == "Drive",
            _ => true
        };
    }

    private void NotifyShellChange()
    {
        if (_shellRefreshService.TryRefresh())
        {
            SetStatus("Изменения применены. Если не видите — перезапустите Проводник.");
        }
        else
        {
            SetStatus("Не удалось обновить проводник. Перезапустите его вручную.");
        }
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        ShowStatusMessage = !string.IsNullOrWhiteSpace(message);
    }

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task EditItemAsync()
    {
        if (SelectedItem == null)
        {
            return;
        }

        if (SelectedItem.IsSystemItem)
        {
            SetStatus("Системные элементы нельзя редактировать.");
            return;
        }

        if (SelectedItem.IsLegacyHandler || SelectedItem.IsModernHandler)
        {
            SetStatus("Этот элемент не поддерживает редактирование.");
            return;
        }

        var editModel = await _itemEditDialogService.ShowEditDialogAsync(SelectedItem);
        if (editModel == null)
        {
            return;
        }

        var result = await _registryService.UpdateContextMenuItemAsync(
            SelectedItem.RegistryPath,
            editModel.DisplayName,
            editModel.IconPath,
            editModel.Command);

        if (result.Success)
        {
            SelectedItem.DisplayName = editModel.DisplayName;
            SelectedItem.IconPath = editModel.IconPath;
            SelectedItem.Command = editModel.Command;
            ApplyFilters();
            NotifyShellChange();
        }
        else
        {
            SetStatus(result.Error ?? "Не удалось обновить элемент.");
        }
    }

}
