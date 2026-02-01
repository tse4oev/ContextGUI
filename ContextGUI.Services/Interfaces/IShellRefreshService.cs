namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides shell refresh capabilities for Explorer.
/// </summary>
public interface IShellRefreshService
{
    /// <summary>
    /// Attempts to refresh Windows shell to apply context menu changes.
    /// </summary>
    /// <returns>True when refresh request was sent.</returns>
    bool TryRefresh();
}
