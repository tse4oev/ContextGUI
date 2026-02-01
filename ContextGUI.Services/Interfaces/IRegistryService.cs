using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;

namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides context menu operations against the Windows Registry.
/// </summary>
public interface IRegistryService
{
    /// <summary>
    /// Returns all context menu items from modern and legacy locations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of context menu items.</returns>
    Task<IReadOnlyList<ContextMenuItem>> GetAllContextMenuItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a context menu item by registry path.
    /// </summary>
    /// <param name="keyPath">Full registry key path to disable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result with backup path.</returns>
    Task<RegistryResult<bool>> DisableContextMenuItemAsync(string keyPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a context menu item by registry path.
    /// </summary>
    /// <param name="keyPath">Full registry key path to enable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result with backup path.</returns>
    Task<RegistryResult<bool>> EnableContextMenuItemAsync(string keyPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a context menu item by registry path.
    /// </summary>
    /// <param name="keyPath">Full registry key path to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result with backup path.</returns>
    Task<RegistryResult<bool>> DeleteContextMenuItemAsync(string keyPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a context menu item by registry path.
    /// </summary>
    /// <param name="keyPath">Full registry key path to update.</param>
    /// <param name="displayName">New display name.</param>
    /// <param name="iconPath">New icon path or null to clear.</param>
    /// <param name="command">New command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result with backup path.</returns>
    Task<RegistryResult<bool>> UpdateContextMenuItemAsync(string keyPath, string displayName, string? iconPath, string? command, CancellationToken cancellationToken = default);
}
