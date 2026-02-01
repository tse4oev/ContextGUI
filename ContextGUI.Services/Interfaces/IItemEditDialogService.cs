namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides dialog for editing context menu items.
/// </summary>
public interface IItemEditDialogService
{
    /// <summary>
    /// Shows edit dialog for the specified item.
    /// </summary>
    /// <param name="item">Item to edit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Edited data or null if cancelled.</returns>
    Task<ContextGUI.Models.ContextMenuItemEditModel?> ShowEditDialogAsync(ContextGUI.Models.ContextMenuItem item, CancellationToken cancellationToken = default);
}
