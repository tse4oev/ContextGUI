using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;

namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides access to registry backup history.
/// </summary>
public interface IBackupHistoryService
{
    /// <summary>
    /// Returns available backups, newest first.
    /// </summary>
    Task<IReadOnlyList<BackupEntry>> GetBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the latest backup if available.
    /// </summary>
    Task<RegistryResult<bool>> RestoreLatestBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the specified backup file.
    /// </summary>
    Task<RegistryResult<bool>> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}
