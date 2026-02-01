using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;
using ContextGUI.Services.Interfaces;

namespace ContextGUI.Services;

/// <summary>
/// Provides backup history and restore operations.
/// </summary>
public sealed class BackupHistoryService : IBackupHistoryService
{
    private readonly IAdminService _adminService;
    private readonly ILoggingService _logger;

    public BackupHistoryService(IAdminService adminService, ILoggingService logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public Task<IReadOnlyList<BackupEntry>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<BackupEntry>();
        var backupDirectory = GetBackupDirectory();

        if (!Directory.Exists(backupDirectory))
        {
            return Task.FromResult<IReadOnlyList<BackupEntry>>(list);
        }

        foreach (var file in Directory.GetFiles(backupDirectory, "*.reg"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileInfo = new FileInfo(file);
            list.Add(new BackupEntry
            {
                FilePath = fileInfo.FullName,
                FileName = fileInfo.Name,
                CreatedAt = fileInfo.CreationTime,
                SourceHint = TryGetSourceHint(fileInfo.Name)
            });
        }

        var ordered = list.OrderByDescending(x => x.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<BackupEntry>>(ordered);
    }

    public async Task<RegistryResult<bool>> RestoreLatestBackupAsync(CancellationToken cancellationToken = default)
    {
        var backups = await GetBackupsAsync(cancellationToken);
        var latest = backups.FirstOrDefault();
        if (latest == null)
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Backups not found."
            };
        }

        return await RestoreBackupAsync(latest.FilePath, cancellationToken);
    }

    public async Task<RegistryResult<bool>> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator())
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Administrator privileges are required."
            };
        }

        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            return new RegistryResult<bool>
            {
                Success = false,
                Error = "Backup file not found."
            };
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = $"import \"{backupFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogError(new InvalidOperationException(error), "Registry restore failed");
                return new RegistryResult<bool>
                {
                    Success = false,
                    Error = "Registry restore failed."
                };
            }

            _logger.LogInformation("Registry restored from backup: {0}", backupFilePath);

            return new RegistryResult<bool>
            {
                Success = true,
                Value = true,
                BackupPath = backupFilePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registry restore failed");
            return new RegistryResult<bool>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static string GetBackupDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ContextGUI",
            "Backups");
    }

    private static string? TryGetSourceHint(string fileName)
    {
        var parts = fileName.Split('_');
        if (parts.Length < 3)
        {
            return null;
        }

        var raw = string.Join("_", parts.Skip(2));
        return raw.Replace(".reg", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
