using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Services.Interfaces;

namespace ContextGUI.Services;

/// <summary>
/// Creates registry backups using the reg.exe export command.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IAdminService _adminService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="adminService">Admin service.</param>
    public BackupService(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(string keyPath, CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator())
        {
            throw new UnauthorizedAccessException("Administrator privileges are required.");
        }

        if (string.IsNullOrWhiteSpace(keyPath))
        {
            throw new ArgumentException("Key path is required.", nameof(keyPath));
        }

        var backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ContextGUI",
            "Backups");

        Directory.CreateDirectory(backupDirectory);

        var backupFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{SanitizePath(keyPath)}.reg";
        var backupPath = Path.Combine(backupDirectory, backupFileName);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "reg",
                Arguments = $"export \"{keyPath}\" \"{backupPath}\" /y",
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
            throw new InvalidOperationException($"Registry backup failed: {error}");
        }

        return backupPath;
    }

    private static string SanitizePath(string path)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            path = path.Replace(c, '_');
        }

        return path.Replace('\\', '_').Replace('/', '_');
    }
}
