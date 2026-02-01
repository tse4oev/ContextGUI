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
    private readonly IBackupHistoryService _backupHistoryService;
    private readonly IShellRefreshService _shellRefreshService;
    private readonly IAdminService _adminService;
    private readonly ISettingsService _settingsService;
    private readonly IUpdateService _updateService;
    private readonly IUpdateDialogService _updateDialogService;
    private readonly ILoggingService _logger;
    private AppSettings _settings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="registryService">Registry service.</param>
    /// <param name="itemEditDialogService">Item edit dialog service.</param>
    /// <param name="backupHistoryService">Backup history service.</param>
    /// <param name="shellRefreshService">Shell refresh service.</param>
    /// <param name="logger">Logging service.</param>
    public MainViewModel(
        IRegistryService registryService,
        IItemEditDialogService itemEditDialogService,
        IBackupHistoryService backupHistoryService,
        IShellRefreshService shellRefreshService,
        IAdminService adminService,
        ISettingsService settingsService,
        IUpdateService updateService,
        IUpdateDialogService updateDialogService,
        ILoggingService logger)
    {
        _registryService = registryService;
        _itemEditDialogService = itemEditDialogService;
        _backupHistoryService = backupHistoryService;
        _shellRefreshService = shellRefreshService;
        _adminService = adminService;
        _settingsService = settingsService;
        _updateService = updateService;
        _updateDialogService = updateDialogService;
        _logger = logger;
        ContextMenuItems.CollectionChanged += (_, _) => ApplyFilters();
        ApplyFilters();
        OnSelectedPageChanged(SelectedPage);
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

    [ObservableProperty]
    private ObservableCollection<BackupEntry> _backups = new();

    [ObservableProperty]
    private ObservableCollection<string> _recentEvents = new();

    [ObservableProperty]
    private AppPage _selectedPage = AppPage.Main;

    [ObservableProperty]
    private bool _isMainPage = true;

    [ObservableProperty]
    private bool _isDonatePage;

    [ObservableProperty]
    private string _authorName = "TOKYO";

    [ObservableProperty]
    private string _donateTitle = "Поддержать автора";

    [ObservableProperty]
    private string _donateMessage = "Спасибо, что выбрали эту программу. Ваша поддержка помогает развивать проект.";

    [ObservableProperty]
    private string _donateFooter = "С уважением,";

    [ObservableProperty]
    private string _donateLink = "https://dalink.to/tokyo_dev";

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private string _updateStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _showUpdateStatus;

    [ObservableProperty]
    private string _repositoryOwner = string.Empty;

    [ObservableProperty]
    private string _repositoryName = string.Empty;

    partial void OnBackupsChanged(ObservableCollection<BackupEntry> value)
    {
        RestoreLatestBackupCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Loads initial data for the main view.
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        await LoadContextMenuItemsAsync();

        if (AutoCheckUpdates)
        {
            await CheckUpdatesAsync(silent: true);
        }
    }

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

    partial void OnSelectedPageChanged(AppPage value)
    {
        IsMainPage = value == AppPage.Main;
        IsDonatePage = value == AppPage.Donate;
    }

    partial void OnAutoCheckUpdatesChanged(bool value)
    {
        _settings.Update.AutoCheckUpdates = value;
        _ = SaveSettingsAsync();
    }

    partial void OnRepositoryOwnerChanged(string value)
    {
        _settings.Update.RepositoryOwner = value?.Trim();
        _ = SaveSettingsAsync();
    }

    partial void OnRepositoryNameChanged(string value)
    {
        _settings.Update.RepositoryName = value?.Trim();
        _ = SaveSettingsAsync();
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
            await LoadBackupsAsync();
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

    [RelayCommand]
    private async Task LoadBackupsAsync()
    {
        try
        {
            var items = await _backupHistoryService.GetBackupsAsync();
            Backups.Clear();
            foreach (var item in items)
            {
                Backups.Add(item);
            }

            RestoreLatestBackupCommand.NotifyCanExecuteChanged();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to load backups");
        }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        await CheckUpdatesAsync(silent: false);
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

    [RelayCommand(CanExecute = nameof(CanRestoreBackup))]
    private async Task RestoreLatestBackupAsync()
    {
        var result = await _backupHistoryService.RestoreLatestBackupAsync();
        if (result.Success)
        {
            NotifyShellChange();
            await LoadContextMenuItemsAsync();
        }
        else
        {
            SetStatus(result.Error ?? "Не удалось восстановить backup.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRestoreSelectedBackup))]
    private async Task RestoreSelectedBackupAsync(BackupEntry? backup)
    {
        if (backup == null)
        {
            return;
        }

        var result = await _backupHistoryService.RestoreBackupAsync(backup.FilePath);
        if (result.Success)
        {
            NotifyShellChange();
            await LoadContextMenuItemsAsync();
        }
        else
        {
            SetStatus(result.Error ?? "Не удалось восстановить backup.");
        }
    }

    private bool CanRestoreBackup() => Backups.Count > 0;

    private bool CanRestoreSelectedBackup(BackupEntry? backup) => backup != null;

    private void SetStatus(string message)
    {
        StatusMessage = message;
        ShowStatusMessage = !string.IsNullOrWhiteSpace(message);
        if (!string.IsNullOrWhiteSpace(message))
        {
            AddEvent(message);
        }
    }

    private void AddEvent(string message)
    {
        var stamp = System.DateTime.Now.ToString("HH:mm");
        RecentEvents.Insert(0, $"{stamp} — {message}");
        while (RecentEvents.Count > 10)
        {
            RecentEvents.RemoveAt(RecentEvents.Count - 1);
        }
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

    private async Task LoadSettingsAsync()
    {
        _settings = await _settingsService.LoadAsync();
        AutoCheckUpdates = _settings.Update.AutoCheckUpdates;
        RepositoryOwner = _settings.Update.RepositoryOwner ?? string.Empty;
        RepositoryName = _settings.Update.RepositoryName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_settings.Update.RepositoryOwner))
        {
            _settings.Update.RepositoryOwner = "tse4oev";
        }

        if (string.IsNullOrWhiteSpace(_settings.Update.RepositoryName))
        {
            _settings.Update.RepositoryName = "ContextGUI";
        }

        RepositoryOwner = _settings.Update.RepositoryOwner;
        RepositoryName = _settings.Update.RepositoryName;

        await SaveSettingsAsync();
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveAsync(_settings);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    private async Task CheckUpdatesAsync(bool silent)
    {
        var settings = _settings.Update;
        var currentVersion = _updateService.GetCurrentVersion();

        var result = await _updateService.CheckForUpdateAsync(settings);
        if (!result.Success || result.Value == null)
        {
            if (!silent)
            {
                SetUpdateStatus(result.Error ?? "Не удалось проверить обновления.");
            }

            return;
        }

        var latest = result.Value;
        if (latest.Version <= currentVersion)
        {
            if (!silent)
            {
                SetUpdateStatus("У вас установлена последняя версия.");
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.SkipVersion) &&
            string.Equals(settings.SkipVersion, latest.Version.ToString(), System.StringComparison.OrdinalIgnoreCase))
        {
            if (!silent)
            {
                SetUpdateStatus("Эта версия обновления скрыта.");
            }

            return;
        }

        var dialogResult = await _updateDialogService.ShowUpdateDialogAsync(latest, currentVersion);
        if (dialogResult == UpdateDialogResult.SkipVersion)
        {
            settings.SkipVersion = latest.Version.ToString();
            await SaveSettingsAsync();
            SetUpdateStatus("Версия обновления скрыта.");
            return;
        }

        if (dialogResult == UpdateDialogResult.OpenRelease && !string.IsNullOrWhiteSpace(latest.ReleaseUrl))
        {
            OpenUrl(latest.ReleaseUrl);
            SetUpdateStatus("Открыта страница релиза.");
            return;
        }

        if (!silent)
        {
            SetUpdateStatus("Обновление доступно.");
        }
    }

    private void SetUpdateStatus(string message)
    {
        UpdateStatusMessage = message;
        ShowUpdateStatus = !string.IsNullOrWhiteSpace(message);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(info);
        }
        catch
        {
        }
    }
}
