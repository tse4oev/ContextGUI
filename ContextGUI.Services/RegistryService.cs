using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;
using ContextGUI.Services.Interfaces;
using Microsoft.Win32;

namespace ContextGUI.Services;

/// <summary>
/// Provides safe registry operations for Windows 11 context menus.
/// </summary>
public sealed class RegistryService : IRegistryService
{
    private static readonly string[] BasePaths =
    [
        @"*\shell",
        @"AllFilesystemObjects\shell",
        @"Directory\shell",
        @"Directory\Background\shell",
        @"Folder\shell",
        @"Drive\shell",
        @"*\shellex\ContextMenuHandlers",
        @"AllFilesystemObjects\shellex\ContextMenuHandlers",
        @"Folder\shellex\ContextMenuHandlers",
        @"Directory\Background\shellex\ContextMenuHandlers"
    ];

    private const string ClassesRootPrefix = @"HKEY_CLASSES_ROOT\";

    private readonly IRegistryWrapper _registry;
    private readonly IBackupService _backupService;
    private readonly IAdminService _adminService;
    private readonly ILoggingService _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryService"/> class.
    /// </summary>
    /// <param name="registry">Registry wrapper.</param>
    /// <param name="backupService">Backup service.</param>
    /// <param name="adminService">Admin service.</param>
    /// <param name="logger">Logging service.</param>
    public RegistryService(IRegistryWrapper registry, IBackupService backupService, IAdminService adminService, ILoggingService logger)
    {
        _registry = registry;
        _backupService = backupService;
        _adminService = adminService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContextMenuItem>> GetAllContextMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator())
        {
            _logger.LogWarning("GetAllContextMenuItemsAsync called without administrator privileges.");
            return Array.Empty<ContextMenuItem>();
        }

        return await Task.Run(() => LoadItems(cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RegistryResult<bool>> DisableContextMenuItemAsync(string keyPath, CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator())
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Administrator privileges are required."
            };
        }

        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Registry key path is required."
            };
        }

        if (!keyPath.StartsWith(ClassesRootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = $"Only {ClassesRootPrefix} paths are supported."
            };
        }

        var backupResult = await CreateBackupOrFailAsync(keyPath, cancellationToken);
        if (!backupResult.Success)
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = backupResult.Error
            };
        }

        var backupPath = backupResult.Value!;

        try
        {
            var subKeyPath = keyPath[ClassesRootPrefix.Length..];
            using var key = _registry.OpenClassesRootSubKey(subKeyPath, writable: true);
            if (key == null)
            {
                return new RegistryResult<bool>
                {
                    Success = false,
                    Error = $"Registry key not found: {keyPath}",
                    BackupPath = backupPath
                };
            }

            key.SetValue("LegacyDisable", string.Empty, RegistryValueKind.String);
            _logger.LogInformation("Disabled context menu item: {0}", keyPath);

            return new RegistryResult<bool>
            {
                Success = true,
                Value = true,
                BackupPath = backupPath
            };
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security error while disabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while disabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while disabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while disabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
    }

    private List<ContextMenuItem> LoadItems(CancellationToken cancellationToken)
    {
        var items = new List<ContextMenuItem>();

        foreach (var basePath in BasePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var baseKey = _registry.OpenClassesRootSubKey(basePath);
                if (baseKey == null)
                {
                    continue;
                }

                foreach (var subKeyName in baseKey.GetSubKeyNames())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var subKey = baseKey.OpenSubKey(subKeyName);
                    if (subKey == null)
                    {
                        continue;
                    }

                    var item = CreateItem(basePath, subKeyName, subKey);
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to read registry path {0}: {1}", basePath, ex.Message);
            }
        }

        return items;
    }

    private static ContextMenuItem CreateItem(string basePath, string subKeyName, IRegistryKey subKey)
    {
        var displayName = subKey.GetValue(string.Empty)?.ToString();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = subKey.GetValue("MUIVerb")?.ToString();
        }

        displayName ??= subKeyName;
        var registryPath = $"{ClassesRootPrefix}{basePath}\\{subKeyName}";
        var iconPath = subKey.GetValue("Icon")?.ToString();
        var (command, isModernHandler) = TryGetCommandOrHandler(subKey);
        var isLegacyHandler = basePath.Contains(@"\shellex\ContextMenuHandlers", StringComparison.OrdinalIgnoreCase);

        return new ContextMenuItem
        {
            Name = subKeyName,
            DisplayName = displayName,
            RegistryPath = registryPath,
            IsEnabled = subKey.GetValue("LegacyDisable") == null,
            IconPath = iconPath,
            IsSystemItem = IsSystemItem(subKeyName),
            Category = GetCategoryFromPath(basePath),
            Command = command,
            IsLegacyHandler = isLegacyHandler,
            IsModernHandler = isModernHandler
        };
    }

    private static (string? Command, bool IsModernHandler) TryGetCommandOrHandler(IRegistryKey subKey)
    {
        using var commandKey = subKey.OpenSubKey("command");
        var command = commandKey?.GetValue(string.Empty)?.ToString();
        var delegateExecute = subKey.GetValue("DelegateExecute")?.ToString();
        var explorerCommandHandler = subKey.GetValue("ExplorerCommandHandler")?.ToString();
        var isModern = !string.IsNullOrWhiteSpace(delegateExecute) || !string.IsNullOrWhiteSpace(explorerCommandHandler);

        if (!string.IsNullOrWhiteSpace(command))
        {
            return (command, isModern);
        }

        if (isModern)
        {
            var handlerText = BuildHandlerText(delegateExecute, explorerCommandHandler);
            return (handlerText, true);
        }

        return (null, false);
    }

    private static string GetCategoryFromPath(string basePath)
    {
        if (basePath.StartsWith(@"AllFilesystemObjects\", StringComparison.OrdinalIgnoreCase))
        {
            return "All Files";
        }

        if (basePath.StartsWith(@"Directory\Background", StringComparison.OrdinalIgnoreCase))
        {
            return "Folder Background";
        }

        if (basePath.StartsWith(@"Folder\", StringComparison.OrdinalIgnoreCase))
        {
            return "Folder";
        }

        if (basePath.StartsWith(@"Directory\", StringComparison.OrdinalIgnoreCase))
        {
            return "Folder";
        }

        if (basePath.StartsWith(@"Drive\", StringComparison.OrdinalIgnoreCase))
        {
            return "Drive";
        }

        if (basePath.StartsWith(@"*\", StringComparison.OrdinalIgnoreCase))
        {
            return "All Files";
        }

        return "Other";
    }

    /// <inheritdoc />
    public async Task<RegistryResult<bool>> EnableContextMenuItemAsync(string keyPath, CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator())
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Administrator privileges are required."
            };
        }

        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Registry key path is required."
            };
        }

        if (!keyPath.StartsWith(ClassesRootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = $"Only {ClassesRootPrefix} paths are supported."
            };
        }

        var backupResult = await CreateBackupOrFailAsync(keyPath, cancellationToken);
        if (!backupResult.Success)
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = backupResult.Error
            };
        }

        var backupPath = backupResult.Value!;

        try
        {
            var subKeyPath = keyPath[ClassesRootPrefix.Length..];
            using var key = _registry.OpenClassesRootSubKey(subKeyPath, writable: true);
            if (key == null)
            {
                return new RegistryResult<bool>
                {
                    Success = false,
                    Error = $"Registry key not found: {keyPath}",
                    BackupPath = backupPath
                };
            }

            key.DeleteValue("LegacyDisable", throwOnMissingValue: false);
            _logger.LogInformation("Enabled context menu item: {0}", keyPath);

            return new RegistryResult<bool>
            {
                Success = true,
                Value = true,
                BackupPath = backupPath
            };
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security error while enabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while enabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while enabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while enabling item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
    }

    /// <inheritdoc />
    public async Task<RegistryResult<bool>> DeleteContextMenuItemAsync(string keyPath, CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator())
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Administrator privileges are required."
            };
        }

        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Registry key path is required."
            };
        }

        if (!keyPath.StartsWith(ClassesRootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = $"Only {ClassesRootPrefix} paths are supported."
            };
        }

        var backupResult = await CreateBackupOrFailAsync(keyPath, cancellationToken);
        if (!backupResult.Success)
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = backupResult.Error
            };
        }

        var backupPath = backupResult.Value!;

        try
        {
            var subKeyPath = keyPath[ClassesRootPrefix.Length..];
            var lastSeparator = subKeyPath.LastIndexOf('\\');
            if (lastSeparator <= 0 || lastSeparator == subKeyPath.Length - 1)
            {
                return new RegistryResult<bool>
                {
                    Success = false,
                    Error = $"Invalid registry key path: {keyPath}",
                    BackupPath = backupPath
                };
            }

            var parentPath = subKeyPath[..lastSeparator];
            var subKeyName = subKeyPath[(lastSeparator + 1)..];

            using var parentKey = _registry.OpenClassesRootSubKey(parentPath, writable: true);
            if (parentKey == null)
            {
                return new RegistryResult<bool>
                {
                    Success = false,
                    Error = $"Registry key not found: {keyPath}",
                    BackupPath = backupPath
                };
            }

            parentKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false);
            _logger.LogInformation("Deleted context menu item: {0}", keyPath);

            return new RegistryResult<bool>
            {
                Success = true,
                Value = true,
                BackupPath = backupPath
            };
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security error while deleting item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while deleting item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while deleting item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
    }

    /// <inheritdoc />
    public async Task<RegistryResult<bool>> UpdateContextMenuItemAsync(string keyPath, string displayName, string? iconPath, string? command, CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator())
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Administrator privileges are required."
            };
        }

        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Registry key path is required."
            };
        }

        if (!keyPath.StartsWith(ClassesRootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = $"Only {ClassesRootPrefix} paths are supported."
            };
        }

        if (keyPath.Contains(@"\shellex\ContextMenuHandlers", StringComparison.OrdinalIgnoreCase))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Legacy handlers do not support editing."
            };
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Display name is required."
            };
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Command is required."
            };
        }

        var backupResult = await CreateBackupOrFailAsync(keyPath, cancellationToken);
        if (!backupResult.Success)
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = backupResult.Error
            };
        }

        var backupPath = backupResult.Value!;

        try
        {
            var subKeyPath = keyPath[ClassesRootPrefix.Length..];
            using var key = _registry.OpenClassesRootSubKey(subKeyPath, writable: true);
            if (key == null)
            {
                return new RegistryResult<bool>
                {
                    Success = false,
                    Error = $"Registry key not found: {keyPath}",
                    BackupPath = backupPath
                };
            }

            var isModernHandler = key.GetValue("DelegateExecute") != null || key.GetValue("ExplorerCommandHandler") != null;
            if (isModernHandler)
            {
                return new RegistryResult<bool>
                {
                    Success = false,
                    Error = "Modern handlers do not support editing.",
                    BackupPath = backupPath
                };
            }

            key.SetValue(string.Empty, displayName, RegistryValueKind.String);

            if (string.IsNullOrWhiteSpace(iconPath))
            {
                key.DeleteValue("Icon", throwOnMissingValue: false);
            }
            else
            {
                key.SetValue("Icon", iconPath, RegistryValueKind.String);
            }

            using var commandKey = key.OpenSubKey("command", writable: true) ?? key.CreateSubKey("command");
            commandKey.SetValue(string.Empty, command, RegistryValueKind.String);

            _logger.LogInformation("Updated context menu item: {0}", keyPath);

            return new RegistryResult<bool>
            {
                Success = true,
                Value = true,
                BackupPath = backupPath
            };
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security error while updating item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while updating item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while updating item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating item: {0}", keyPath);
            return FailureFromException(ex, backupPath);
        }
    }

    private static bool IsSystemItem(string subKeyName)
    {
        return subKeyName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)
               || subKeyName.Equals("OpenWith", StringComparison.OrdinalIgnoreCase)
               || subKeyName.Equals("Sharing", StringComparison.OrdinalIgnoreCase);
    }

    private static RegistryResult<bool> FailureFromException(Exception ex, string backupPath)
    {
        return new RegistryResult<bool>
        {
            Success = false,
            Error = ex.Message,
            BackupPath = backupPath
        };
    }

    private async Task<RegistryResult<string>> CreateBackupOrFailAsync(string keyPath, CancellationToken cancellationToken)
    {
        try
        {
            var backupPath = await _backupService.CreateBackupAsync(keyPath, cancellationToken);
            return new RegistryResult<string>
            {
                Success = true,
                Value = backupPath,
                BackupPath = backupPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed for {0}", keyPath);
            return new RegistryResult<string>
            {
                Success = false,
                Error = $"Backup failed: {ex.Message}"
            };
        }
    }

    private static string BuildHandlerText(string? delegateExecute, string? explorerCommandHandler)
    {
        if (!string.IsNullOrWhiteSpace(delegateExecute) && !string.IsNullOrWhiteSpace(explorerCommandHandler))
        {
            return $"DelegateExecute: {delegateExecute} | ExplorerCommandHandler: {explorerCommandHandler}";
        }

        if (!string.IsNullOrWhiteSpace(delegateExecute))
        {
            return $"DelegateExecute: {delegateExecute}";
        }

        if (!string.IsNullOrWhiteSpace(explorerCommandHandler))
        {
            return $"ExplorerCommandHandler: {explorerCommandHandler}";
        }

        return string.Empty;
    }
}
