using System.Threading;
using System.Threading.Tasks;

namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides registry backup operations.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a backup for the specified registry key path.
    /// </summary>
    /// <param name="keyPath">Full registry key path to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full path to the created backup file.</returns>
    Task<string> CreateBackupAsync(string keyPath, CancellationToken cancellationToken = default);
}
